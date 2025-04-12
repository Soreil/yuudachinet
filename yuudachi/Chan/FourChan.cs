using System.Text.Json;

using yuudachi.Chan.DTO;

namespace yuudachi.Chan;

public class FourChan
{
    private HttpClient Client { get; }

    const string boardsAPI = @"https://a.4cdn.org/";
    const string boards = @"boards.json";

    public FourChan()
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
}
