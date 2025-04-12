using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record ThreadsOnPageDTO(
    [property: JsonPropertyName("page")] int PageNumber, 
    [property: JsonPropertyName("threads")] List<ThreadDescriptorDTO> Threads);
