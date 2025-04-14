using System.Text.Json.Serialization;

namespace yuudachi.Groq;

public record Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content)
{
    public static Message NewUserMessage(string content) => new("user", content);
    public static Message NewSystemMessage(string content) => new("system", content);
}
