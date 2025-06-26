namespace yuudachi.Groq;

public class GroqConversationHistory
{
    public Dictionary<ulong, ConversationWithFormattingOptions> Conversations { get; } = [];
}

public class ConversationWithFormattingOptions
{
    public required Conversation Conversation { get; init; }
    public required IFormattingOptions FormattingOptions { get; init; }
}

public interface IFormattingOptions
{
    public IEnumerable<string> Format(string content);
}

public class TruncatingFormatter : IFormattingOptions
{
    private readonly int maxLength;
    public TruncatingFormatter(int maxLength)
    {
        this.maxLength = maxLength;
    }
    public IEnumerable<string> Format(string content)
    {
        if (content.Length > maxLength)
        {
            return [string.Concat(content.AsSpan(0, maxLength), "... (truncated)")];
        }
        return [content];
    }
}

public class NaiveChunkingFormatter : IFormattingOptions
{
    private readonly int chunkSize;
    public NaiveChunkingFormatter(int chunkSize)
    {
        this.chunkSize = chunkSize;
    }
    public IEnumerable<string> Format(string content)
    {
        return (IEnumerable<string>)content.Chunk(chunkSize);
    }
}