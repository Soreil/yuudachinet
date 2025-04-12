using System.Text.Json.Serialization;

namespace yuudachi.Models;

record Dj([property: JsonPropertyName("id")] int ID,
          [property: JsonPropertyName("djname")] string Djname,
          [property: JsonPropertyName("djtext")] string Djtext,
          [property: JsonPropertyName("djimage")] string Djimage,
          [property: JsonPropertyName("djcolor")] string Djcolor,
          [property: JsonPropertyName("visible")] bool Visible,
          [property: JsonPropertyName("priority")] int Priority,
          [property: JsonPropertyName("css")] string CSS,
          [property: JsonPropertyName("theme_id")] int ThemeID,
          [property: JsonPropertyName("role")] string Role);
