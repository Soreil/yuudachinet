using NetCord;
using NetCord.Rest;

using System.Text.Json;

namespace yuudachi.Radio;

public class RadioClient
{
    const string api = @"https://r-a-d.io/api";
    const string frontpage = @"https://r-a-d.io";
    const int radioRed = 0xDF4C3A;

    public RadioClient()
    {
        Client = new HttpClient() { BaseAddress = new Uri(frontpage) };
    }

    public async Task<EmbedProperties?> GetUpcomingAsEmbed()
    {
        var upcoming = await GetCurrentState();
        if (upcoming == null)
            return null;

        if (!upcoming.Main.Isafkstream)
        {
            return new EmbedProperties()
            {
                Title = "Upcoming Songs",
                Url = frontpage,
                Color = new(radioRed),
                Description = "A DJ is playing so there is no queue.",
            };
        }

        var DjImageUrl = $"{api}/dj-image/{upcoming.Main.Dj.Djimage}";

        var now = DateTimeOffset.FromUnixTimeSeconds(upcoming.Main.Current);


        List<EmbedFieldProperties> fields = [.. upcoming.Main.Queue.Select(x => {
                    var start = DateTimeOffset.FromUnixTimeSeconds(x.Timestamp);

                    var time = $"{now - start:mm\\:ss}";

            return new EmbedFieldProperties() {
            Name = $"{time} from now",
            Value = x.Meta,
            Inline=false
            }; })];

        var embed = new EmbedProperties()
        {
            Url = frontpage,
            Title = "Playback queue",
            Color = new(radioRed),
            Footer = new EmbedFooterProperties()
            {
                Text = $"Now playing: {upcoming.Main.Np}"
            },
            Author = new()
            {
                IconUrl = DjImageUrl,
                Name = $"{upcoming.Main.Dj.Djname}"
            },
            Thumbnail = new(DjImageUrl),
            Fields = fields
        };

        return embed;
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

        EmbedFooterProperties? footer = null;
        EmbedImageProperties? image = null;

        if (current.Main.Isafkstream && !HasImage(current.Main.Thread))
        {
            footer = new EmbedFooterProperties()
            {
                Text = current.Main.Isafkstream ?
            $"Upcoming: {current.Main.Queue[0].Meta}" :
            $"Current thread: {current.Main.Thread}"
            };
        }
        else
        {
            image = new EmbedImageProperties(GetImageURL(current.Main.Thread));
        }

        var embed = new EmbedProperties
        {
            Image = image,
            Title = "Now Playing",
            Url = frontpage,
            Color = new(radioRed),
            Footer = footer,
            Thumbnail = new($"{api}/dj-image/{current.Main.Dj.Djimage}"),
            Fields = [.. fields]

        };

        return embed;
    }

    private static string? GetImageURL(string thread)
    {
        var index = thread.IndexOf(':');
        if (index == -1) return thread;
        var url = thread[(index + 1)..].Trim();
        return url;
    }

    private static bool HasImage(string thread)
    {
        if (!Uri.IsWellFormedUriString(thread, UriKind.RelativeOrAbsolute)) return false;

        try
        {
            var uri = new Uri(thread, UriKind.RelativeOrAbsolute);

            if (!uri.IsFile) return false;

            var fileName = uri.LocalPath;
            var fileExtension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return imageExtensions.Contains(fileExtension);
        }
        catch
        {
            return false;
        }
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