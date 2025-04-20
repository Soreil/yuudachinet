using System.Text.Json.Serialization;

namespace yuudachi.Groq;

public record GroqResponse(
    [property: JsonPropertyName("id")] string ID,
    [property: JsonPropertyName("object")] string Object,
    [property: JsonPropertyName("created")] int Created,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] List<Choice> Choices,
    [property: JsonPropertyName("usage")] UsageEntry Usage,
    [property: JsonPropertyName("system_fingerprint")] string SystemFingerprint,
    [property: JsonPropertyName("x_groq")] XGroqEntry XGroq);

public record Choice(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("message")] Message Message,
    [property: JsonPropertyName("logprobs")] object Logprobs,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

public record UsageEntry(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("prompt_time")] double PromptTime,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("completion_time")] double CompletionTime,
    [property: JsonPropertyName("total_tokens")] int TotalTokens,
    [property: JsonPropertyName("total_time")] double TotalTime);

public record XGroqEntry([property: JsonPropertyName("id")] string ID);
