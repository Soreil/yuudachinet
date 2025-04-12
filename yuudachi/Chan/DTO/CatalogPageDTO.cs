using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record CatalogPageDTO(
    [property: JsonPropertyName("page")] int PageNumber,
    [property: JsonPropertyName("threads")] List<ThreadDTO> Threads);
