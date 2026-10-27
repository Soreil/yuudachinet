using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

using System.ClientModel;

using yuudachi.Deepseek;

namespace yuudachi;


public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, ConversationHistory convoHistory, DeepSeekClient deepseekClient) : IMessageCreateGatewayHandler
{
    private const string errorPrefix = "Oopsie woopsie we got an error deepseek sisters: ";

    public async ValueTask HandleAsync(NetCord.Gateway.Message message)
    {
        if (message.Author.IsBot)
            return;

        if (message.ReferencedMessage is not null && convoHistory.Conversations.TryGetValue(message.ReferencedMessage.Id, out Conversation? convo))
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                logger.LogWarning("Received an empty DeepSeek follow-up message {MessageId}", message.Id);
                await message.ReplyAsync(new ReplyMessageProperties { Content = "Please include a text message for DeepSeek." });
                return;
            }

            Conversation completed;
            try
            {
                completed = await deepseekClient.ReplyAsync(convo, message.Content, new(message.Author.Id, message.GuildId));
            }
            catch (DeepSeekUsageTrackingException ex)
            {
                logger.LogError(ex, "Could not save DeepSeek usage for message {MessageId}", message.Id);
                await message.ReplyAsync(new ReplyMessageProperties { Content = "Usage tracking failed. The API request may already have completed; please contact the bot administrator before retrying." });
                return;
            }
            catch (Exception ex) when (ex is ClientResultException or HttpRequestException or OperationCanceledException or InvalidOperationException)
            {
                logger.LogError(ex, "Error while calling DeepSeek API");
                await message.ReplyAsync(new ReplyMessageProperties { Content = $"{errorPrefix}Could not get a response from DeepSeek. Please try again." });
                return;
            }

            var reply = await message.ReplyAsync(new ReplyMessageProperties { Content = completed.DiscordReply });
            convoHistory.Conversations[reply.Id] = completed;
        }

        else
        {
            var words = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Any(x => x.Equals("poi", StringComparison.InvariantCultureIgnoreCase)))
            {
                await message.SendAsync(new MessageProperties()
                {
                    Content = "Poi!",
                });

                if (message.Guild is not null)
                {
                    var emojis = await message.Guild.GetEmojisAsync();
                    var poiEmotes = emojis.Where(x => x.Name.Contains("poi", StringComparison.InvariantCultureIgnoreCase))
                          .ToList();

                    foreach (var emote in poiEmotes)
                    {
                        await message.AddReactionAsync(new ReactionEmojiProperties(emote.Name, emote.Id));
                    }
                }
            }
        }

        return;
    }
}