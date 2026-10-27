using Microsoft.Extensions.AI;

namespace yuudachi.Deepseek;

public class Conversation
{
    public IReadOnlyList<ChatMessage> Messages { get; }
    public float Temperature { get; }

    public Conversation(string? systemPrompt = null, float temperature = 0.6f)
    {
        Messages = string.IsNullOrWhiteSpace(systemPrompt) ? [] : [new(ChatRole.System, systemPrompt)];
        Temperature = temperature;
    }

    internal Conversation(List<ChatMessage> messages, float temperature)
    {
        Messages = messages.AsReadOnly();
        Temperature = temperature;
    }

    public string DiscordReply
    {
        get
        {
            const string truncationWarning = "... (truncated)";
            var content = Messages.Last().Text;
            return content.Length <= 2000 ? content : content[..(2000 - truncationWarning.Length)] + truncationWarning;
        }
    }
}