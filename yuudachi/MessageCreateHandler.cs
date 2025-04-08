//ViI8ZVPvuxxVlOUIwisZjzmVLuGw_IOg
using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace yuudachi;

[GatewayEvent(nameof(GatewayClient.MessageCreate))]
public class MessageCreateHandler(ILogger<MessageCreateHandler> logger) : IGatewayEventHandler<Message>
{
    public ValueTask HandleAsync(Message message)
    {
        logger.LogInformation("{}", message.Content);
        return default;
    }
}