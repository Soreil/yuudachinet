using System.Text.Json.Serialization;

namespace yuudachi.Groq;

public class GroqModels
{
    [JsonPropertyName("object")]
    public string? Object { get;set; }
    [JsonPropertyName("data")]
    public List<GroqModelDescriptor> Data { get; set; } = [];

}

public class GroqModelDescriptor
{
    /*      "id": "gemma2-9b-it",
      "object": "model",
      "created": 1693721698,
      "owned_by": "Google",
      "active": true,
      "context_window": 8192,
      "public_apps": null
*/

    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("object")]
    public string? Object { get; set; }
    [JsonPropertyName("created")]
    public long Created { get; set; }
    [JsonPropertyName("owned_by")]
    public string? OwnedBy { get; set; }
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("context_window")]
    public int ContextWindow { get; set; }
    [JsonPropertyName("public_apps")]
    public object? PublicApps { get; set; }
}