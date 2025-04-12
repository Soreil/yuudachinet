using NetCord.Services.ApplicationCommands;
using Microsoft.Extensions.Logging;
using yuudachi.Chan;
using Microsoft.Extensions.Configuration;

namespace yuudachi;

[SlashCommand("4chan", "4chan tools")]
public class ChanCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{

    private ILogger<ChanCommandsModule> Logger { get; }
    public FourChanClient Chan { get; }

    public ChanCommandsModule(ILogger<ChanCommandsModule> logger, FourChanClient fourChanClient)
    {
        Logger = logger;
        Chan = fourChanClient;

    }
    [SubSlashCommand("random", "Gets a random thread")]
    public async Task<string> Post([SlashCommandParameter(Name = "board", ChoicesProviderType = typeof(FourChanBoardPicker))] string board)
    {
        var page = await Chan.GetBoardIndexPage(board, 1);
        if (page == null)
        {
            return "Error getting 4chan data";

        }

        var thread = Random.Shared.GetItems([.. page.Threads], 1)[0];
        Logger.LogInformation("Thread: {thread}", thread);

        return thread.Posts.First().Comment;
    }
}
