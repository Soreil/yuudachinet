using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;

using System.Text.Json;
using System.Text.RegularExpressions;

using yuudachi.Chan.DTO;

namespace yuudachi.Chan;

public partial class FourChanClient
{
    private HttpClient Client { get; }
    public ILogger<FourChanClient> Logger { get; }

    const string APIRoot = @"https://a.4cdn.org/";
    const string ImageRoot = @"https://i.4cdn.org/";
    const string StaticRoot = @"https://s.4cdn.org/";
    const string BoardsRoot = @"https://boards.4chan.org/";
    const string boards = @"boards.json";

    const int GifBanners = 253;
    const int PngBanners = 262;
    const int JpgBanners = 224;
    private readonly string[] FileTypes = ["jpg", "png", "gif"];

    public readonly Color YotsubaBlue = new(0xEEF2FF);
    public readonly Color YotsubaRed = new(0xFFFFEE);
    public readonly Color YotsubaGreen = new(0x35B214);

    public T MakeThreadEmbedStructure<T>(ThreadRepliesDTO thread, string board) where T : IMessageProperties, new()
    {
        var openingPost = thread.Posts[0];

        var footerText = thread.Posts.Count switch
        {
            1 => "There are no replies ;_;",
            2 => "There is one reply.",
            _ => $"There are {thread.Posts.Count - 1} replies."
        };

        var bannerURL = GetRandomBannerURL();
        var imageURL = GetImageUrl(board, openingPost.Tim, openingPost.Extension!);
        var threadURL = GetThreadURL(board, openingPost.Number);

        var comment = CleanUpText(openingPost.Comment ?? "");
        var subject = CleanUpText(openingPost.Subject ?? "link");

        Logger.LogInformation("Comment: {comment}", comment);
        Logger.LogInformation("Subject: {subject}", subject);

        var author = openingPost.Country switch
        {
            null or "" => new EmbedAuthorProperties()
            {
                Name = openingPost.Name ?? "Anonymous",
            },
            _ => new EmbedAuthorProperties()
            {
                Name = openingPost.Name ?? "Anonymous",
                IconUrl = GetCountryImageURL(openingPost.Country),
            }
        };

        if (typeof(T) == typeof(InteractionMessageProperties))
        {
            return new T()
            {
                Embeds = [new EmbedProperties()
            {
                Url = threadURL,
                Title = subject,
                Color = YotsubaGreen,
                Footer = new EmbedFooterProperties()
                {
                    Text = footerText
                },
                Author = author,
                Thumbnail = bannerURL,
                Description = comment,
                Image = new EmbedImageProperties(imageURL),
            }]
            };
        }
        else if (typeof(T) == typeof(MessageProperties))
        {
            return new T()
            {
                Embeds = [new EmbedProperties()
            {
                Url = threadURL,
                Title = subject,
                Color = YotsubaGreen,
                Footer = new EmbedFooterProperties()
                {
                    Text = footerText
                },
                Author = author,
                Thumbnail = bannerURL,
                Description = comment,
                Image = new EmbedImageProperties(imageURL),
            }]
            };
        }


        else throw new Exception($"Unsupported type {typeof(T)} for MakeThreadEmbedStructure");
    }

    public string GetRandomBannerURL()
    {
        var rng = new Random();
        var res = rng.GetItems([.. FileTypes], 3)[0];

        return res switch
        {
            "jpg" => $@"{StaticRoot}/image/title/{rng.Next(0, JpgBanners)}.jpg",
            "png" => $@"{StaticRoot}/image/title/{rng.Next(0, PngBanners)}.png",
            "gif" => $@"{StaticRoot}/image/title/{rng.Next(0, GifBanners)}.gif",
            _ => throw new ArgumentOutOfRangeException(nameof(res), res, null)
        };
    }

    public FourChanClient(ILogger<FourChanClient> logger)
    {
        Client = new HttpClient()
        {
            BaseAddress = new Uri(APIRoot),
            DefaultRequestHeaders = {
                { "User-Agent", "Yuudachi" },
                { "Accept", "application/json" }
            }
        };
        Logger = logger;
    }

    public static string GetImageUrl(string board, long timestamp, string ext) => $"{ImageRoot}{board}/{timestamp}{ext}";

    public static string GetThreadURL(string board, long thread) => $"{BoardsRoot}{board}/thread/{thread}";

    public async Task<List<BoardDTO>> GetBoards()
    {
        var response = await Client.GetAsync(boards);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var boards = JsonSerializer.Deserialize<BoardsDTO>(json);
            return boards == null ? [] : boards.Boards;
        }
        else
        {
            return [];
        }
    }

    public async Task<List<CatalogPageDTO>> GetCatalog(string board)
    {
        var response = await Client.GetAsync($@"{board}/catalog.json");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var catalog = JsonSerializer.Deserialize<List<CatalogPageDTO>>(json);
            return catalog ?? [];
        }
        else
        {
            return [];
        }
    }

    public async Task<List<ThreadsOnPageDTO>> GetThreadsOnPage(string board)
    {
        var response = await Client.GetAsync($@"{board}/threads.json");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var threads = JsonSerializer.Deserialize<List<ThreadsOnPageDTO>>(json);
            return threads ?? [];
        }
        else
        {
            return [];
        }
    }

    public async Task<BoardIndexPageDTO?> GetBoardIndexPage(string board, int pageNumber)
    {
        var response = await Client.GetAsync($@"{board}/{pageNumber}.json");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var thread = JsonSerializer.Deserialize<BoardIndexPageDTO>(json);
            return thread;
        }
        else
        {
            return null;
        }
    }

    public async Task<ThreadRepliesDTO?> TryGetThread(string board, long threadId)
    {
        var response = await Client.GetAsync($@"{board}/thread/{threadId}.json");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var thread = JsonSerializer.Deserialize<ThreadRepliesDTO>(json);
            return thread;
        }
        else
        {
            return null;
        }
    }

    internal static string GetCountryImageURL(string country) => $@"{StaticRoot}/image/country/{country.ToLowerInvariant()}.gif";

    /// <summary>
    /// Remove HTML from string with Regex.
    /// </summary>
    public static string CleanUpText(string source)
    {
        source = source.Replace("<br>", "\n");
        source = source.Replace("<wbr>", "\n");
        source = source.Replace("<br/>", "\n");
        source = source.Replace("<wbr/>", "\n");
        var decoded = System.Net.WebUtility.HtmlDecode(source);

        decoded = decoded.Replace("&amp;", "&");
        decoded = decoded.Replace(@"&#039;", @"'");
        decoded = decoded.Replace(@"&gt;", @">");
        decoded = decoded.Replace(@"&lt;", @"<");


        var cleaned = RemoveHTMLRegex().Replace(decoded, string.Empty);
        return cleaned;
    }

    [GeneratedRegex("<.*?>")]
    private static partial Regex RemoveHTMLRegex();

}
