using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record ReplyDTO(
    [property: JsonPropertyName("no")] int Number,
    [property: JsonPropertyName("now")] string Now,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("com")] string Comment,
    [property: JsonPropertyName("filename")] string? FileName,
    [property: JsonPropertyName("ext")] string? Extension,
    [property: JsonPropertyName("w")] int Width,
    [property: JsonPropertyName("h")] int Height,
    [property: JsonPropertyName("tn_w")] int ThumbnailWidth,
    [property: JsonPropertyName("tn_h")] int ThumbnailHeight,
    [property: JsonPropertyName("tim")] long Tim,
    [property: JsonPropertyName("time")] int Time,
    [property: JsonPropertyName("md5")] string? MD5,
    [property: JsonPropertyName("fsize")] int FileSize,
    [property: JsonPropertyName("resto")] int ThreadID);
