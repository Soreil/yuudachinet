using System.Text.Json.Serialization;

namespace yuudachi.Radio;

public record Current([property: JsonPropertyName("main")] Main Main);
