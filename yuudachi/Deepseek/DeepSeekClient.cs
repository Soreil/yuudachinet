using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using System.ClientModel;

namespace yuudachi.Deepseek;

public class DeepSeekClient(IChatClient chatClient, ILogger<DeepSeekClient> logger, DeepSeekUsageStore usageStore)
{
    public async Task<Conversation> ReplyAsync(Conversation conversation, string question, DeepSeekUsageIdentity identity, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentOutOfRangeException.ThrowIfZero(identity.UserId);
        cancellationToken.ThrowIfCancellationRequested();
        List<ChatMessage> messages = [.. conversation.Messages, new(ChatRole.User, question)];
        ChatResponse response;
        try
        {
            response = await chatClient.GetResponseAsync(messages,
                new ChatOptions { Temperature = conversation.Temperature }, cancellationToken);
        }
        catch (Exception ex) when (ex is ClientResultException or HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            await usageStore.RecordAsync(identity, messages, null, failed: true);
            throw;
        }

        await usageStore.RecordAsync(identity, messages, response, failed: string.IsNullOrWhiteSpace(response.Text));

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            logger.LogWarning("DeepSeek returned an empty chat response.");
            throw new InvalidOperationException("No response received from DeepSeek.");
        }

        messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
        return new Conversation(messages, conversation.Temperature);
    }
}
