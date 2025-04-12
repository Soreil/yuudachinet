using NetCord.Rest;

using System.Text.Json;

using yuudachi.Models;

namespace yuudachi;

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