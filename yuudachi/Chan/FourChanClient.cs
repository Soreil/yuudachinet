using System.Text.Json;

using yuudachi.Chan.DTO;

namespace yuudachi.Chan;

public class FourChanClient
{
    private HttpClient Client { get; }

    const string boardsAPI = @"https://a.4cdn.org/";
    const string boards = @"boards.json";

    public FourChanClient()
    {
        Client = new HttpClient()
        {
            BaseAddress = new Uri(boardsAPI),
            DefaultRequestHeaders = {
                { "User-Agent", "Yuudachi" },
                { "Accept", "application/json" }
            }
        };
    }

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
}
