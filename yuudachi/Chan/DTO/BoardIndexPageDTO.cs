using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record BoardIndexPageDTO([property: JsonPropertyName("threads")] List<ThreadRepliesDTO> Threads);
