
using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using System.Text.RegularExpressions;

using yuudachi.Chan;
using yuudachi.Chan.DTO;

namespace yuudachi.Commands;

[SlashCommand("4chan", "4chan tools")]
public partial class ChanCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private ILogger<ChanCommandsModule> Logger { get; }
    public FourChanClient Chan { get; }


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

        return Chan.MakeThreadEmbedStructure<InteractionMessageProperties>(thread, board);
    }
}
