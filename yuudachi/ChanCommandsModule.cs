
using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using System.Text.RegularExpressions;

using yuudachi.Chan;
using yuudachi.Chan.DTO;

namespace yuudachi;

[SlashCommand("4chan", "4chan tools")]
public partial class ChanCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private ILogger<ChanCommandsModule> Logger { get; }
    public FourChanClient Chan { get; }

    public readonly Color YotsubaBlue = new(0xEEF2FF);
    public readonly Color YotsubaRed = new(0xFFFFEE);
    public readonly Color YotsubaGreen = new(0x35B214);

    public ChanCommandsModule(ILogger<ChanCommandsModule> logger, FourChanClient fourChanClient)
    {
        Logger = logger;
        Chan = fourChanClient;
    }

    [SubSlashCommand("random", "Gets a random thread")]
    public async Task<InteractionMessageProperties> Post([SlashCommandParameter(Name = "board")] string board)
    {
        var page = await Chan.GetBoardIndexPage(board, 1);
        if (page == null)
        {
            return "Error getting 4chan data";
        }

        var thread = Random.Shared.GetItems([.. page.Threads], 1).First();

        Logger.LogInformation("Thread: {thread}", thread);

        return MakeThreadEmbedStructure(thread, board);
    }

    private InteractionMessageProperties MakeThreadEmbedStructure(ThreadRepliesDTO thread, string board)
    {
        var openingPost = thread.Posts[0];

        var footerText = thread.Posts.Count switch
        {
            1 => "There are no replies ;_;",
            2 => "There is one reply.",
            _ => $"There are {thread.Posts.Count - 1} replies."
        };

        var bannerURL = Chan.GetRandomBannerURL();
        var imageURL = FourChanClient.GetImageUrl(board, openingPost.Tim, openingPost.Extension!);
        var threadURL = FourChanClient.GetThreadURL(board, openingPost.Number);

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
                IconUrl = FourChanClient.GetCountryImageURL(openingPost.Country),
            }
        };

        var result = new InteractionMessageProperties()
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

        return result;
    }

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
