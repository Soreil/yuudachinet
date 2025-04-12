using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record Cooldowns(
    [property: JsonPropertyName("threads")] int Threads,
    [property: JsonPropertyName("replies")] int Replies,
    [property: JsonPropertyName("images")] int Images
);