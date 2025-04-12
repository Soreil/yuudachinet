using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

//The UNIX timestamp marking the last time the thread was modified (post added/modified/deleted, thread closed/sticky settings modified)	
public record ThreadDescriptorDTO(
    [property: JsonPropertyName("no")] int Number, 
    [property: JsonPropertyName("last_modified")] int LastModified, 
    [property: JsonPropertyName("replies")] int Replies);
