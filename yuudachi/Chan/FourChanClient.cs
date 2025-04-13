using NetCord;

using System.Text.Json;

using yuudachi.Chan.DTO;

namespace yuudachi.Chan;

public class FourChanClient
{
    private HttpClient Client { get; }

    const string APIRoot = @"https://a.4cdn.org/";
    const string ImageRoot = @"https://i.4cdn.org/";
    const string StaticRoot = @"https://s.4cdn.org/";
    const string boards = @"boards.json";

    const int GifBanners = 253;
    const int PngBanners = 262;
    const int JpgBanners = 224;
    private readonly string[] FileTypes = ["jpg", "png", "gif"];

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

    public FourChanClient()
    {
        Client = new HttpClient()
        {
            BaseAddress = new Uri(APIRoot),
            DefaultRequestHeaders = {
                { "User-Agent", "Yuudachi" },
                { "Accept", "application/json" }
            }
        };
    }

    public static string GetImageUrl(string board, long timestamp, string ext) => $"{ImageRoot}{board}/{timestamp}{ext}";

    public static string GetThreadURL(string board, int thread) => $"{ImageRoot}{board}/thread/{thread}";

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

    public async Task<ThreadRepliesDTO?> TryGetThread(string board, int threadId)
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
}
