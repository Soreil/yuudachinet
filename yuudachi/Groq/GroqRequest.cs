using System.Text.Json;
using System.Text.Json.Serialization;

namespace yuudachi.Groq;

public record GroqRequest(
    [property: JsonPropertyName("messages")] List<Message> Messages,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("reasoning_format")] string? ReasoningFormat = null,
    [property: JsonPropertyName("max_completion_tokens")] int? MaxCompletionTokens = null)
{
    public string Serialize() => JsonSerializer.Serialize(this);
}
