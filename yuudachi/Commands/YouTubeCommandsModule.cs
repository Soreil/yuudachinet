using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace yuudachi.Commands;

[SlashCommand("youtube", "YouTube commands")]
public partial class YouTubeCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ILogger<YouTubeCommandsModule> logger;

    public YoutubeClientKey APIKey { get; }
    public YoutubeResponses YoutubeResponses { get; }
    public YouTubeService YouTubeService { get; }

    public YouTubeCommandsModule(ILogger<YouTubeCommandsModule> logger, IOptions<YoutubeClientKey> APIKey, YoutubeResponses youtubeResponses)
    {
        this.logger = logger;
        this.APIKey = APIKey.Value;
        this.YoutubeResponses = youtubeResponses;

        this.YouTubeService = CreateYouTubeService();
    }

    [SubSlashCommand("search", "Search for a YouTube video")]
    public async Task GetYouTubeInfo([SlashCommandParameter(Description = "Search query")] string query)
    {
        var request = YouTubeService.Search.List(new List<string>() { "id", "snippet" });
        request.Q = query;
        request.MaxResults = 5;
        request.Type = "video";

        var response = await request.ExecuteAsync();

        var videoItems = response.Items.Where(x => x.Id.Kind == "youtube#video").ToList();

        if (videoItems.Count == 0)
        {
            var error = InteractionCallback.Message(new()
            {
                Content = "No video found for the given query."
            });
            _ = await RespondAsync(error);
        }

        var videoStrings = videoItems.Select(x => $"https://www.youtube.com/watch?v={x.Id.VideoId}\n").ToList();
        //Random.Shared.Shuffle(CollectionsMarshal.AsSpan(videoStrings));

        var browser = new YouTubeSearchResultBrowser(videoStrings);

        var body = browser.GetCurrentVideo();

        var msg = new InteractionMessageProperties()
        {
            Content = body,
            Components = [
                new ActionRowProperties(
                    [
                        new ButtonProperties("youtubePrevious","Previous Video", ButtonStyle.Primary),
                        new ButtonProperties("youtubeNext","Next Video", ButtonStyle.Primary)
                     ]
                    )]
        };

        var callback = InteractionCallback.Message(msg);

        var responseMessage = await RespondAsync(callback,true);

        if (responseMessage is not null)
        {
            YoutubeResponses.Responses.Add(new(responseMessage, browser));
        }
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
