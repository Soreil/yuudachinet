using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace yuudachi;

[GatewayEvent(nameof(GatewayClient.MessageCreate))]
public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, Groq.GroqConversationHistory convoHistory) : IGatewayEventHandler<Message>
{
    public async ValueTask HandleAsync(Message message)
    {
        logger.LogInformation("{}", message.Content);

        if (message.ReferencedMessage is not null && convoHistory.Conversations.ContainsKey(message.ReferencedMessage.Id))
        {
            var oldConvo = convoHistory.Conversations[message.ReferencedMessage.Id];

            var body = "Cool reply king!";

            var resp = await message.ReplyAsync(new ReplyMessageProperties() { Content = body });

            convoHistory.Conversations.Add(resp.Id, oldConvo);
        }

        return;
    }
}