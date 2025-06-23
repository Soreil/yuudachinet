using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using yuudachi.Groq;
using Microsoft.Extensions.Options;

namespace yuudachi.ChoiceProviders;

public class GroqModelPicker : IChoicesProvider<ApplicationCommandContext>
{
    public static GroqClient? Client { get; set; }
    private List<GroqModelDescriptor>? Models { get; set; }

    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        SlashCommandParameter<ApplicationCommandContext> parameter)
    {
        if (Client == null)
            return new();
        if (Models == null || Models.Count == 0)
            Models = Client.GetAllModels().Result.Data;

        List<ApplicationCommandOptionChoiceProperties> choices =
            [.. Models.Where(x => x.Active).Select(
                x => new ApplicationCommandOptionChoiceProperties($"{x.Id} by {x.OwnedBy}", x.Id!))];

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
            choices
            .Take(25)
            );
    }
}
public class GroqToolModelPicker : IChoicesProvider<ApplicationCommandContext>
{
    public static GroqClient? Client { get; set; }
    private Dictionary<string, GroqModelDescriptor>? Models { get; set; }

    public static IOptions<GroqToolModels>? Options { get; set; }

    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        SlashCommandParameter<ApplicationCommandContext> parameter)
    {
        if (Client == null)
            return new();
        if (Models == null || Models.Count == 0)
        {
            Models = Options.Value.Models.Select(x => (x, Client.TryGetModel(x).Result))
                .OfType<(string, GroqModelDescriptor)>()
                .Where(x => x.Item2.Active)
                .ToDictionary(x => x.Item1, x => x.Item2);
        }
        ;

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(
            Models.Select(x => new ApplicationCommandOptionChoiceProperties($"{x.Value.Id} by {x.Value.OwnedBy}", x.Value.Id!))
            );
    }
}