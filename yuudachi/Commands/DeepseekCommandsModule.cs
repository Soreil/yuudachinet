using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using System.Text;
using System.ClientModel;
using System.Text.Json;

using yuudachi.Deepseek;

namespace yuudachi.Commands;

[SlashCommand("deepseek", "DeepSeek commands")]
public class DeepseekCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly DeepSeekClient deepseekClient;
    private readonly ConversationHistory conversationHistory;
    private readonly DeepSeekUsageStore usageStore;
    private readonly DeepSeekSearchClient searchClient;
    private readonly ILogger<DeepseekCommandsModule> logger;
    private const string errorPrefix = "Oopsie woopsie we got an error deepseek sisters: ";

    public string? DefaultPrompt { get; private set; } = "You are tool which summarizes conversations. Please summarize the conversation history provided to you in a short message.";

    public DeepseekCommandsModule(DeepSeekClient deepseekClient, ConversationHistory conversationHistory, ILogger<DeepseekCommandsModule> logger, DeepSeekUsageStore usageStore, DeepSeekSearchClient searchClient)
    {
        this.deepseekClient = deepseekClient;
        this.conversationHistory = conversationHistory;
        this.logger = logger;
        this.usageStore = usageStore;
        this.searchClient = searchClient;
    }


    [SubSlashCommand("ask", "Ask a question")]
    public async Task Ask(
        [SlashCommandParameter(Description = "Your very interesting query", MinLength = 1)] string question)
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        await AnswerAsync(new Conversation(), question);
    }

    [SubSlashCommand("summarize", "Summarizes the last posts in the channel")]
    public async Task Summarize(
        [SlashCommandParameter(Description = "Optional prompt")] string? prompt = null,
        [SlashCommandParameter(Description = "Number of messages to summarize, default is 100", MinValue = 2, MaxValue = 1000)] int numMessages = 100,
        [SlashCommandParameter(Description = "Temperature controls how creative the answers are, 0.0 is rigid and 2.0 is extremely cooked", MinValue = 0.0, MaxValue = 2.0)] double temp = 0.6)
    {
        var lastMessage = Context.Channel.LastMessageId;
        if (lastMessage is null)
        {
            logger.LogWarning("No messages found in channel {ChannelId}", Context.Channel.Id);
            _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}No messages found in this channel"), false);
            return;
        }

        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        var paginator = Context.Channel.GetMessagesAsync(new PaginationProperties<ulong>() { BatchSize = 100, Direction = PaginationDirection.Before, From = lastMessage.Value });

        var messages = await paginator.Take(numMessages).ToListAsync();

        if (messages.Count == 0)
        {
            logger.LogWarning("No messages available to summarize in channel {ChannelId}", Context.Channel.Id);
            await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}No messages found in this channel"));
            return;
        }

        var conversationSummary = await ConversationSummary.CreateAsync(messages, 15000);

        string input = conversationSummary.SummarizeToText();
        await AnswerAsync(new Conversation(prompt ?? DefaultPrompt, (float)temp), input);
    }

    [SubSlashCommand("search", "Search the web and answer using current sources")]
    public async Task Search(
        [SlashCommandParameter(Description = "Search query; supports site:, quoted phrases, and date filters", MinLength = 1, MaxLength = 128)] string query)
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        DeepSeekSearchResults results;
        try
        {
            results = await searchClient.SearchAsync(query, new(Context.User.Id, Context.Interaction.GuildId));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid DeepSeek search query");
            await ModifyResponseAsync(properties => properties.WithContent("Please enter a non-blank search query of at most 128 characters."));
            return;
        }
        catch (DeepSeekUsageTrackingException ex)
        {
            logger.LogError(ex, "Could not save DeepSeek search usage");
            await ModifyResponseAsync(properties => properties.WithContent("Usage tracking failed. The search may already have completed; please contact the bot administrator before retrying."));
            return;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            logger.LogError(ex, "Error while calling DeepSeek Search API");
            var error = ex switch
            {
                HttpRequestException { StatusCode: System.Net.HttpStatusCode.PaymentRequired } => "DeepSeek search is unavailable because the API account has insufficient balance. Please contact the bot administrator.",
                HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden } => "DeepSeek rejected access to search. Please contact the bot administrator.",
                HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } => "DeepSeek search is rate limited. Please try again later.",
                OperationCanceledException => "DeepSeek search timed out or was cancelled. Please try again later.",
                _ => "Could not search the web with DeepSeek. Please try again later."
            };
            await ModifyResponseAsync(properties => properties.WithContent(error));
            return;
        }

        if (results.Results.Count == 0)
        {
            await ModifyResponseAsync(properties => properties.WithContent("No web results were found for that query."));
            return;
        }
        await AnswerAsync(results.StartConversation(), results.Prompt, results.SourceEmbeds);
    }

    [SubSlashCommand("usage", "Show DeepSeek call and token usage for this server, or yourself in DMs")]
    public async Task Usage()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral), true);
        try
        {
            var viewer = new DeepSeekUsageIdentity(Context.User.Id, Context.Interaction.GuildId);
            var users = await usageStore.GetUsageAsync(viewer);
            await ModifyResponseAsync(properties => properties.WithEmbeds([DeepSeekUsageReport.Create(users, viewer)]));
        }
        catch (DeepSeekUsageTrackingException ex)
        {
            logger.LogError(ex, "Could not read DeepSeek usage");
            await ModifyResponseAsync(properties => properties.WithContent("Could not read usage tracking data. Please contact the bot administrator."));
        }
    }

    private async Task AnswerAsync(Conversation conversation, string question, IEnumerable<EmbedProperties>? sources = null)
    {
        Conversation completed;
        try
        {
            completed = await deepseekClient.ReplyAsync(conversation, question, new(Context.User.Id, Context.Interaction.GuildId));
        }
        catch (DeepSeekUsageTrackingException ex)
        {
            logger.LogError(ex, "Could not save DeepSeek usage");
            await ModifyResponseAsync(properties => properties.WithContent("Usage tracking failed. The API request may already have completed; please contact the bot administrator before retrying."));
            return;
        }
        catch (Exception ex) when (ex is ClientResultException or HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogError(ex, "Error while calling DeepSeek API");
            await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}Could not get a response from DeepSeek. Please try again."));
            return;
        }

        var reply = sources is null
            ? await ModifyResponseAsync(x => x.WithContent(completed.DiscordReply))
            : await ModifyResponseAsync(x => x.WithContent(completed.DiscordReply).WithEmbeds(sources));
        conversationHistory.Conversations[reply.Id] = completed;
    }
}

internal class ConversationSummary
{
    public required int MaxTokens { get; init; }

    public static async Task<ConversationSummary> CreateAsync(IEnumerable<RestMessage> messages, int maxTokens = 10000)
    {
        var messagesInChronologicalOrder = messages
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var userMessageTasks = messagesInChronologicalOrder
            .Select(async x => new UserMessage(x, await UserMessage.GetReactions(x)))
            .ToList();

        var userMessages = await Task.WhenAll(userMessageTasks);

        return new ConversationSummary([.. userMessages]) { MaxTokens = maxTokens };
    }

    internal string SummarizeToText()
    {
        var timeElapsed = UserMessages.Last().CreatedAt - UserMessages.First().CreatedAt;

        var builder = new StringBuilder();
        builder.AppendLine("Conversation Summary:");
        builder.AppendLine($"Total Messages: {UserMessages.Count}");
        builder.AppendLine($"Total conversation duration:{timeElapsed}");
        builder.AppendLine("Messages:");

        int charactersPerMessage = MaxTokens / UserMessages.Count / 2;

        foreach (var mesage in UserMessages)
        {
            var messageBuilder = new StringBuilder();

            var includeTime = charactersPerMessage > 50; // If we have enough space, include the time in the message

            var timemsg = includeTime ? $" at {mesage.CreatedAt:g}" : "";

            if (mesage.ReferencedMessageAuthor is not null)
            {
                messageBuilder.AppendLine($"{mesage.Username} replied to {mesage.ReferencedMessageAuthor}{timemsg}: { [.. mesage.Content.Take(charactersPerMessage)]}");
            }

            else
            {
                messageBuilder.AppendLine($"{mesage.Username}{timemsg}: { [.. mesage.Content.Take(charactersPerMessage)]}");
            }

            if (messageBuilder.Length < charactersPerMessage)
            {
                foreach (var react in mesage.Reactions)
                {
                    messageBuilder.AppendLine($"{react.Count} users reacted with {react.EmojiName}");
                    messageBuilder.AppendLine($"Users: {string.Join(", ", react.Usernames)}");
                    if (messageBuilder.Length >= charactersPerMessage)
                    {
                        break;
                    }
                }
            }

            builder.AppendLine(messageBuilder.ToString().TrimEnd());
        }

        return builder.ToString();
    }

    private ConversationSummary(List<UserMessage> userMessages)
    {
        UserMessages = userMessages;
    }

    public List<UserMessage> UserMessages { get; }
}

internal class UserMessage
{
    public string Username { get; }
    public string Content { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? ReferencedMessageAuthor { get; }
    public List<Reaction> Reactions { get; }

    public static async Task<List<Reaction>> GetReactions(RestMessage Message)
    {
        List<Reaction> Reactions = [];

        var requestprops = new RestRequestProperties() { RateLimitHandling = RestRateLimitHandling.NoRetry, AuditLogReason = "Fetching reactions for user message while summarizing" };
        foreach (var react in Message.Reactions)
        {
            ReactionEmojiProperties? props = react.Emoji switch
            {
                { Id: null, Name: not null } => new ReactionEmojiProperties(react.Emoji.Name),
                { Id: not null, Name: null } => null, //bug in the API?
                { Id: not null, Name: not null } => new ReactionEmojiProperties(react.Emoji.Name, react.Emoji.Id.Value),
                _ => null
            };

            if (props is not null)
            {
                var usernames = new List<string>();
                try
                {
                    await foreach (var user in Message.GetReactionsAsync(props, properties: requestprops))
                    {
                        usernames.Add(user.Username);
                    }
                }
                catch (NetCord.Rest.RestRateLimitedException)
                {
                    break; // If we hit a rate limit, we stop fetching more usernames
                }

                Reactions.Add(new Reaction(react.Count, props.Name, usernames));
            }
        }
        return Reactions;
    }

    public UserMessage(RestMessage message, List<Reaction> reactions)
    {
        Username = message.Author.Username;
        Content = message.Content;
        CreatedAt = message.CreatedAt;

        ReferencedMessageAuthor = message.ReferencedMessage?.Author.Username;
        Reactions = reactions;
    }
}

public class Reaction
{
    public Reaction(int count, string emojiName, List<string> usernames)
    {
        Count = count;
        EmojiName = emojiName;
        Usernames = usernames;
    }

    public int Count { get; }
    public string EmojiName { get; }
    public List<string> Usernames { get; }
}