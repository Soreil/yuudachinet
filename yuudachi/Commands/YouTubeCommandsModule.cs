
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NetCord.Services.ApplicationCommands;

namespace yuudachi.Commands;

[SlashCommand("youtube", "YouTube commands")]
public partial class YouTubeCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ILogger<YouTubeCommandsModule> logger;

    public YoutubeClientKey APIKey { get; }
    public YouTubeService YouTubeService { get; }

    public YouTubeCommandsModule(ILogger<YouTubeCommandsModule> logger, IOptions<YoutubeClientKey> APIKey)
    {
        this.logger = logger;
        this.APIKey = APIKey.Value;

        this.YouTubeService = CreateYouTubeService();
    }

    [SubSlashCommand("search", "Search for a YouTube video")]
    public async Task<string> GetYouTubeInfo([SlashCommandParameter(Description = "Search query")] string query)
    {
        var request = YouTubeService.Search.List(new List<string>() { "id", "snipped" });
        request.Q = query;
        request.MaxResults = 5;
        request.Type = "video";

        var response = await request.ExecuteAsync();

        var videoItems = response.Items.Where(x => x.Id.Kind == "youtube#video").ToList();

        if (videoItems.Count == 0)
        {
            return "No video found for the given query.";
        }

        var index = Random.Shared.Next(videoItems.Count);

        return $"https://www.youtube.com/watch?v={response.Items[index].Id.VideoId}\n";
    }

    private YouTubeService CreateYouTubeService()
    {
        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = APIKey.Key,
            ApplicationName = "Yuudachi"
        });

        return youtubeService;
    }
}
