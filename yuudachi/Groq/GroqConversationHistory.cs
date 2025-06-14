namespace yuudachi.Groq;

public class GroqConversationHistory
{
    public Dictionary<ulong, Conversation> Conversations { get; } = [];
}