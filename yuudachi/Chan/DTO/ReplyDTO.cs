using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record ReplyDTO(
    [property: JsonPropertyName("no")] int Number,
    [property: JsonPropertyName("sticky")] int IsSticky,
    [property: JsonPropertyName("closed")] int IsClosed,
    [property: JsonPropertyName("now")] string Now,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sub")] string Subject,
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
    [property: JsonPropertyName("resto")] int ThreadID,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("m_img")] int MImage,
    [property: JsonPropertyName("semantic_url")] string SemanticURL,
    [property: JsonPropertyName("country_name")] string CountryName,
    [property: JsonPropertyName("replies")] int Replies,
    [property: JsonPropertyName("images")] int Images
    );
