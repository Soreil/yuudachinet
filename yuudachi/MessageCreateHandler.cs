using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

using yuudachi.Groq;

namespace yuudachi;

[GatewayEvent(nameof(GatewayClient.MessageCreate))]
public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, Groq.GroqConversationHistory convoHistory, GroqClient groqClient) : IGatewayEventHandler<NetCord.Gateway.Message>
{
    public async ValueTask HandleAsync(NetCord.Gateway.Message message)
    {
        logger.LogInformation("{}", message.Content);

        if (message.ReferencedMessage is not null && convoHistory.Conversations.TryGetValue(message.ReferencedMessage.Id, out Conversation? convo))
        {
            convo.AddMessage(message.Content);
            var res = await groqClient.ConversationResult(convo);


            var body = res.Choices[0].Message.Content;

            var resp = await message.ReplyAsync(new ReplyMessageProperties() { Content = body });

            convoHistory.Conversations.Add(resp.Id, convo);
        }

        return;
    }
}