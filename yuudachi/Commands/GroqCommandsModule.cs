using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using System.Text;

using yuudachi.ChoiceProviders;
using yuudachi.Groq;

namespace yuudachi.Commands;

[SlashCommand("groq", "Groq commands")]
public class GroqCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly GroqClient groqClient;
    private readonly GroqConversationHistory groqConversationHistory;
    private readonly ILogger<GroqCommandsModule> logger;
    private string MostRecentModelName;
    private string MostRecentToolModelName;
    private const string errorPrefix = "Oopsie woopsie we got an error groq sisters: ";

    public string? DefaultPrompt { get; private set; } = "You are tool which summarizes conversations. Please summarize the conversation history provided to you in a short message.";

    public GroqCommandsModule(GroqClient groqClient, GroqConversationHistory groqConversationHistory, IOptions<GroqSettingsOptions> settings, ILogger<GroqCommandsModule> logger)
    {
        this.groqClient = groqClient;
        this.groqConversationHistory = groqConversationHistory;
        this.logger = logger;
        MostRecentModelName = settings.Value.DefaultModelName;
        MostRecentToolModelName = settings.Value.DefaultToolModelName;
    }

    /*compound-beta: supports multiple tool calls per request. This system is great for use cases that require multiple web searches or code executions per request.
    compound-beta-mini: supports a single tool call per request. This system is great for use cases that require a single web search or code execution per request. compound-beta-mini has an average of 3x lower latency than compound-beta.
    */
    //[SubSlashCommand("tool", "Uses interactive tools such as web search or code execution")]
    //public async Task Tool(
    //    [SlashCommandParameter(Description = "Your query", MinLength = 1)] string question,
    //    [SlashCommandParameter(Description = "Temperature controls how creative the answers are, 0.0 is rigid and 2.0 is extremely cooked", MinValue = 0.0, MaxValue = 2.0)] double temp = 0.6,
    //    [SlashCommandParameter(ChoicesProviderType = typeof(GroqToolModelPicker))] string? modelName = null)
    //{
    //    if (modelName is null)
    //    {
    //        modelName = MostRecentToolModelName;
    //    }
    //    else
    //    {
    //        MostRecentToolModelName = modelName;
    //    }

    //    var model = await groqClient.TryGetModel(modelName);
    //    if (model == null || model.Id is null)
    //    {
    //        logger.LogWarning("Model not found: {ModelName}", modelName);
    //        _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}Model not found"), false);
    //        return;
    //    }

    //    var convo = GroqClient.StartAgenticToolConversation(model.Id, startingTemperature: temp);
    //    convo.AddMessage(question);

    //    GroqResponse? response;
    //    var loadingResponse = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
    //    try
    //    {
    //        response = await groqClient.ConversationResult(convo);
    //        if (response is null || response.Choices.Count == 0)
    //        {
    //            logger.LogWarning("No response received from Groq for question: {Question}", question);
    //            _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}No response received from Groq"));
    //            return;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex, "Error while calling Groq API");
    //        _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}{ex.Message}"));
    //        return;
    //    }

    //    var truncationWarning = "... (truncated)";
    //    var maxSize = 2000 - truncationWarning.Length;

    //    string content = response.Choices[0].Message.Content;
    //    if (string.IsNullOrWhiteSpace(content))
    //    {
    //        logger.LogWarning("Received empty response from Groq for question: {Question}", question);
    //        content = $"{errorPrefix}No response received from Groq";
    //    }


    //    else if (content.Length > maxSize)
    //    {
    //        logger.LogWarning("Response content too long, truncating for question: {Question}", question);
    //        content = content[..maxSize] + truncationWarning;
    //    }

    //    //var result = new InteractionMessageProperties()
    //    //{
    //    //    Content = content,
    //    //};

    //    var r = await ModifyResponseAsync(x => x.WithContent(content));
    //    //var reply = await RespondAsync(InteractionCallback.Message(result), true);

    //    if (loadingResponse?.Interaction.ResponseMessageId is not null)
    //    {
    //        groqConversationHistory.Conversations.Add(loadingResponse.Interaction.ResponseMessageId.Value, convo);
    //    }
    //    else
    //    {
    //        logger.LogWarning("Failed to get response message ID for Groq conversation");
    //    }
    //}

    [SubSlashCommand("ask", "Ask a question")]
    public async Task Ask(
        [SlashCommandParameter(Description = "Your very interesting query", MinLength = 1)] string question,
        [SlashCommandParameter(Description = "Temperature controls how creative the answers are, 0.0 is rigid and 2.0 is extremely cooked", MinValue = 0.0, MaxValue = 2.0)] double temp = 0.6,
        [SlashCommandParameter(ChoicesProviderType = typeof(GroqModelPicker))] string? modelName = null)
    {
        if (modelName is null)
        {
            modelName = MostRecentModelName;
        }
        else
        {
            MostRecentModelName = modelName;
        }

        var model = await groqClient.TryGetModel(modelName);
        if (model == null || model.Id is null)
        {
            logger.LogWarning("Model not found: {ModelName}", modelName);
            _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}Model not found"), false);
            return;
        }

        var convo = GroqClient.StartConversation(model.Id, startingTemperature: temp);
        convo.AddMessage(question);

        GroqResponse? response;
        var loadingResponse = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        try
        {
            response = await groqClient.ConversationResult(convo);
            if (response is null || response.Choices.Count == 0)
            {
                logger.LogWarning("No response received from Groq for question: {Question}", question);
                _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}No response received from Groq"));
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while calling Groq API");
            _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}{ex.Message}"));
            return;
        }

        var truncationWarning = "... (truncated)";
        var maxSize = 2000 - truncationWarning.Length;

        string content = response.Choices[0].Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("Received empty response from Groq for question: {Question}", question);
            content = $"{errorPrefix}No response received from Groq";
        }


        else if (content.Length > maxSize)
        {
            logger.LogWarning("Response content too long, truncating for question: {Question}", question);
            content = content[..maxSize] + truncationWarning;
        }

        //var result = new InteractionMessageProperties()
        //{
        //    Content = content,
        //};

        var r = await ModifyResponseAsync(x => x.WithContent(content));
        //var reply = await RespondAsync(InteractionCallback.Message(result), true);

        if (loadingResponse?.Interaction.ResponseMessageId is not null)
        {
            groqConversationHistory.Conversations.Add(loadingResponse.Interaction.ResponseMessageId.Value, convo);
        }
        else
        {
            logger.LogWarning("Failed to get response message ID for Groq conversation");
        }
    }

    [SubSlashCommand("modelinfo", "Lists model details")]
    public async Task<InteractionMessageProperties> GetGroqModels([SlashCommandParameter(ChoicesProviderType = typeof(GroqModelPicker))] string modelName)
    {
        var model = await groqClient.TryGetModel(modelName);
        if (model == null)
        {
            return "Model not found";
        }


        var res = new InteractionMessageProperties()
        {
            Embeds = [new EmbedProperties()
            {
                Title = model.Id,
                Description = model.OwnedBy,
                Fields = [
                    new EmbedFieldProperties(){
                    Name = "Context Window",
                    Value = model.ContextWindow.ToString(),
                },
                new EmbedFieldProperties()
                {
                    Name = "Released At",
                    Value = DateTimeOffset.FromUnixTimeSeconds( model.Created).ToString()
                }
            ]
            }]
        };

        return res;
    }

    [SubSlashCommand("summarize", "Summarizes the last posts in the channel")]
    public async Task Summarize(
        [SlashCommandParameter(Description = "Optional prompt")] string? prompt = null,
        [SlashCommandParameter(Description = "Number of messages to summarize, default is 10", MinValue = 2, MaxValue = 1000)] int numMessages = 100,
        [SlashCommandParameter(Description = "Temperature controls how creative the answers are, 0.0 is rigid and 2.0 is extremely cooked", MinValue = 0.0, MaxValue = 2.0)] double temp = 0.6,
        [SlashCommandParameter(ChoicesProviderType = typeof(GroqModelPicker))] string? modelName = null)
    {
        if (modelName is null)
        {
            modelName = MostRecentModelName;
        }
        else
        {
            MostRecentModelName = modelName;
        }

        var model = await groqClient.TryGetModel(modelName);
        if (model == null || model.Id is null)
        {
            logger.LogWarning("Model not found: {ModelName}", modelName);
            _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}Model not found"), false);
            return;
        }

        var lastMessage = Context.Channel.LastMessageId;
        if (lastMessage is null)
        {
            logger.LogWarning("No messages found in channel {ChannelId}", Context.Channel.Id);
            _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}No messages found in this channel"), false);
            return;
        }

        var loadingResponse = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        var paginator = Context.Channel.GetMessagesAsync(new PaginationProperties<ulong>() { BatchSize = 100, Direction = PaginationDirection.Before, From = lastMessage.Value });

        var messages = await paginator.Take(numMessages).ToListAsync();

        var conversationSummary = await ConversationSummary.CreateAsync(messages, 15000);

        string input = conversationSummary.SummarizeToText();
        Conversation convo = new(modelName, temp);
        prompt ??= DefaultPrompt;
        convo.AddMessage($"{prompt}\n{input}");

        GroqResponse? response;
        try
        {
            response = await groqClient.ConversationResult(convo);
            if (response is null || response.Choices.Count == 0)
            {
                logger.LogWarning("No response received from Groq for summarization");
                _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}No response received from Groq"));
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while calling Groq API");
            _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}{ex.Message}"));
            return;
        }

        var truncationWarning = "... (truncated)";
        var maxSize = 2000 - truncationWarning.Length;

        string content = response.Choices[0].Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("Received empty response from Groq for summarization");
            content = $"{errorPrefix}No response received from Groq";
        }


        else if (content.Length > maxSize)
        {
            logger.LogWarning("Response content too long, truncating for summarization");
            content = content[..maxSize] + truncationWarning;
        }

        var r = await ModifyResponseAsync(x => x.WithContent(content));

        if (loadingResponse?.Interaction.ResponseMessageId is not null)
        {
            groqConversationHistory.Conversations.Add(loadingResponse.Interaction.ResponseMessageId.Value, convo);
        }
        else
        {
            logger.LogWarning("Failed to get response message ID for Groq conversation");
        }
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