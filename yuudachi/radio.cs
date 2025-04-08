using NetCord.Rest;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace yuudachi;

record Current([property: JsonPropertyName("main")] Main Main);

record Main([property: JsonPropertyName("np")] string Np,
            [property: JsonPropertyName("listeners")] int Listeners,
            [property: JsonPropertyName("bitrate")] int Bitrate,
            [property: JsonPropertyName("isafkstream")] bool Isafkstream,
            [property: JsonPropertyName("isstreamdesk")] bool Isstreamdesk,
            [property: JsonPropertyName("current")] int Current,
            [property: JsonPropertyName("start_time")] int StartTime,
            [property: JsonPropertyName("end_time")] int EndTime,
            [property: JsonPropertyName("lastset")] string Lastset,
            [property: JsonPropertyName("trackid")] int Trackid,
            [property: JsonPropertyName("thread")] string Thread,
            [property: JsonPropertyName("requesting")] bool Requesting,
            [property: JsonPropertyName("djname")] string Djname,
            [property: JsonPropertyName("dj")] Dj Dj,
            [property: JsonPropertyName("queue")] Queue[] Queue,
            [property: JsonPropertyName("lp")] Lp[] Lp);

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

record Queue([property: JsonPropertyName("meta")] string Meta,
             [property: JsonPropertyName("time")] string Time,
             [property: JsonPropertyName("type")] int Type,
             [property: JsonPropertyName("timestamp")] long Timestamp);

record Lp([property: JsonPropertyName("meta")] string Meta,
          [property: JsonPropertyName("time")] string Time,
          [property: JsonPropertyName("type")] int Type,
          [property: JsonPropertyName("timestamp")] int Timestamp);

class Radio
{
    const string api = @"https://r-a-d.io/api";
    const string frontpage = @"https://r-a-d.io";
    const int radioRed = 0xDF4C3A;

    public Radio()
    {
        Client = new HttpClient() { BaseAddress = new Uri(frontpage) };
    }

    public async Task<EmbedProperties?> GetCurrentAsEmbed()
    {
        var current = await GetCurrentState();
        if (current == null)
            return null;

        var start = DateTimeOffset.FromUnixTimeSeconds(current.Main.StartTime);
        var end = DateTimeOffset.FromUnixTimeSeconds(current.Main.EndTime);
        var now = DateTimeOffset.FromUnixTimeSeconds(current.Main.Current);
        var progress = $"{now - start:mm\\:ss} / {end - start:mm\\:ss}";

        List<EmbedFieldProperties> fields = [
            new EmbedFieldProperties(){
                Name = current.Main.Np,
                Value = progress,
                Inline = false
            },
            new EmbedFieldProperties() {
                Name = current.Main.Dj.Djname,
                Value = $"Listeners: {current.Main.Listeners}",
                Inline = false
            },
        ];

        var footer = new EmbedFooterProperties()
        {
            Text = current.Main.Isafkstream ?
        $"Current thread: {current.Main.Thread}" :
        $"Upcoming: {current.Main.Queue[0].Meta}"
        };

        var embed = new EmbedProperties
        {
            Title = "Now Playing",
            Url = frontpage,
            Color = new(radioRed),
            Footer = footer,
            Thumbnail = new($"{api}/dj-image/{current.Main.Dj.Djimage}"),
            Fields = [.. fields]

        };


        return embed;
    }

    public async Task<Current?> GetCurrentState()
    {
        var response = await Client.GetAsync("api");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Current>(json);
        }
        else
        {
            throw new Exception($"Error: {response.StatusCode}");
        }
    }

    private HttpClient Client { get; }
}