using System.Text.Json.Serialization;

namespace yuudachi.Models;

record Lp([property: JsonPropertyName("meta")] string Meta,
          [property: JsonPropertyName("time")] string Time,
          [property: JsonPropertyName("type")] int Type,
          [property: JsonPropertyName("timestamp")] int Timestamp);
