using System.Text.Json.Serialization;

namespace yuudachi.Models;

record Current([property: JsonPropertyName("main")] Main Main);
