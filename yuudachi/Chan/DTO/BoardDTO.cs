using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record BoardDTO(
    [property: JsonPropertyName("board")] string Board,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("ws_board")] int IsWorkSafe,
    [property: JsonPropertyName("per_page")] int PostsPerPage,
    [property: JsonPropertyName("pages")] int PageCount,
    [property: JsonPropertyName("max_filesize")] int MaximumFileSizeInBytes,
    [property: JsonPropertyName("max_comment_chars")] int MaximumPostLength,
    [property: JsonPropertyName("max_webm_filesize")] int MaximumWebmSizeInBytes,
    [property: JsonPropertyName("bump_limit")] int PostLimit,
    [property: JsonPropertyName("image_limit")] int ImageLimit,
    [property: JsonPropertyName("cooldowns")] Cooldowns Cooldowns,
    [property: JsonPropertyName("meta_description")] string MetaDescription,
    [property: JsonPropertyName("is_archived")] int IsArchived
    );
