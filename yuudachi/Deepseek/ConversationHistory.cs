using System.Collections.Concurrent;

namespace yuudachi.Deepseek;

public class ConversationHistory
{
    public ConcurrentDictionary<ulong, Conversation> Conversations { get; } = new();
}