using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using yuudachi.Chan;
using System.Collections.Immutable;

namespace yuudachi;

public class FourChanBoardPicker : IChoicesProvider<ApplicationCommandContext>
{
    public static FourChanClient? Chan { get; set; }

    private ImmutableSortedDictionary<string, string>? Boards { get; set; }

    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(SlashCommandParameter<ApplicationCommandContext> parameter)
    {
        if (Chan == null)
            return new();
        if (Boards == null || Boards.Count == 0)
            Boards = Chan.GetBoards().Result.ToImmutableSortedDictionary(x => x.Board, x => x.Title);

        List<ApplicationCommandOptionChoiceProperties> choices = [.. Boards.Select(x => new ApplicationCommandOptionChoiceProperties($"{x.Key} - {x.Value}", x.Key))];

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
            choices.Where(x => x.StringValue is "b" or "g")
            );
    }
}