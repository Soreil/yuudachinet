using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

using yuudachi.Groq;

namespace yuudachi;


public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, Groq.GroqConversationHistory convoHistory, GroqClient groqClient) : IMessageCreateGatewayHandler
{
    private const string errorPrefix = "Oopsie woopsie we got an error groq sisters: ";

    public async ValueTask HandleAsync(NetCord.Gateway.Message message)
    {
        if (message.ReferencedMessage is not null && convoHistory.Conversations.TryGetValue(message.ReferencedMessage.Id, out Conversation? convo))
        {
            var question = message.Content;
            convo.AddMessage(question);

            GroqResponse? response;
            try
            {
                response = await groqClient.ConversationResult(convo);
                if (response is null || response.Choices.Count == 0)
                {
                    logger.LogWarning("No response received from Groq for question: {Question}", question);
                    _ = await message.ReplyAsync(new ReplyMessageProperties() { Content = $"{errorPrefix}No response received from Groq" });
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while calling Groq API");
                _ = await message.ReplyAsync(new ReplyMessageProperties() { Content = $"{errorPrefix}{ex.Message}" });
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

            var reply = await message.ReplyAsync(new ReplyMessageProperties() { Content = content });


            convoHistory.Conversations.Add(reply.Id, convo);
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