using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record ThreadRepliesDTO([property: JsonPropertyName("posts")] List<ReplyDTO> Posts);
