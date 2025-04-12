using System.Text.Json;
using System.Text.Json.Serialization;

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

public class BoardIndexPageDTO
{
    [JsonPropertyName("threads")]
    public List<ThreadDTO> Threads { get; set; } = [];
}

public class ThreadsOnPageDTO
{
    [JsonPropertyName("page")]
    public int PageNumber { get; set; }
    [JsonPropertyName("threads")]
    public List<ThreadDescriptorDTO> Threads { get; set; } = [];
}

public class ThreadDescriptorDTO
{
    [JsonPropertyName("no")]
    public int Number { get; set; }
    //The UNIX timestamp marking the last time the thread was modified (post added/modified/deleted, thread closed/sticky settings modified)	
    [JsonPropertyName("last_modified")]
    public int LastModified { get; set; }
    [JsonPropertyName("replies")]
    public int Replies { get; set; }
}

public class CatalogPageDTO
{
    [JsonPropertyName("page")]
    public int PageNumber { get; set; }
    [JsonPropertyName("threads")]
    public List<ThreadDTO> Threads { get; set; } = [];
}

public class ThreadDTO
{
    [JsonPropertyName("no")]
    public int Number { get; set; }
    //MM/DD/YY(Day)HH:MM (:SS on some boards), EST/EDT timezone	
    [JsonPropertyName("now")]
    public string Now { get; set; }
    [JsonPropertyName("sub")]
    public string Subject { get; set; }
    [JsonPropertyName("com")]
    public string Comment { get; set; }
    [JsonPropertyName("filename")]
    public string FileName { get; set; }
    [JsonPropertyName("ext")]
    public string Extension { get; set; }
    [JsonPropertyName("w")]
    public int Width { get; set; }
    [JsonPropertyName("h")]
    public int Height { get; set; }
    [JsonPropertyName("tn_w")]
    public int ThumbnailWidth { get; set; }
    [JsonPropertyName("tn_h")]
    public int ThumbnailHeight { get; set; }
    //Unix timestamp + microtime that an image was uploaded	
    [JsonPropertyName("tim")]
    public long Tim { get; set; }
    //UNIX timestamp the post was created	
    [JsonPropertyName("time")]
    public int Time { get; set; }
    //24 character, packed base64 MD5 hash of file	
    [JsonPropertyName("md5")]
    public string MD5 { get; set; }
    [JsonPropertyName("fsize")]
    public int FileSize { get; set; }
    //For replies: this is the ID of the thread being replied to. For OP: this value is zero	
    [JsonPropertyName("resto")]
    public int Restored { get; set; }
    [JsonPropertyName("bumplimit")]
    public int BumpLimit { get; set; }
    [JsonPropertyName("imagelimit")]
    public int ImageLimit { get; set; }
    [JsonPropertyName("semantic_url")]
    public string SemanticURL { get; set; }
    [JsonPropertyName("replies")]
    public int Replies { get; set; }
    [JsonPropertyName("images")]
    public int Images { get; set; }
    [JsonPropertyName("omitted_posts")]
    public int OmittedPosts { get; set; }
    [JsonPropertyName("omitted_images")]
    public int OmittedImages { get; set; }
    [JsonPropertyName("last_replies")]
    public List<ReplyDTO> LastReplies { get; set; } = [];
    [JsonPropertyName("last_modified")]
    public int LastModified { get; set; }
}

public class ReplyDTO
{
    [JsonPropertyName("no")]
    public int Number { get; set; }
    [JsonPropertyName("now")]
    public string Now { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("com")]
    public string Comment { get; set; }
    [JsonPropertyName("filename")]
    public string? FileName { get; set; }
    [JsonPropertyName("ext")]
    public string? Extension { get; set; }
    [JsonPropertyName("w")]
    public int Width { get; set; }
    [JsonPropertyName("h")]
    public int Height { get; set; }
    [JsonPropertyName("tn_w")]
    public int ThumbnailWidth { get; set; }
    [JsonPropertyName("tn_h")]
    public int ThumbnailHeight { get; set; }
    [JsonPropertyName("tim")]
    public long Tim { get; set; }
    [JsonPropertyName("time")]
    public int Time { get; set; }
    [JsonPropertyName("md5")]
    public string? MD5 { get; set; }
    [JsonPropertyName("fsize")]
    public int FileSize { get; set; }
    [JsonPropertyName("resto")]
    public int Restored { get; set; }
}

public class ThreadRepliesDTO
{
    [JsonPropertyName("posts")]
    public List<ReplyDTO> Posts { get; set; } = [];
}