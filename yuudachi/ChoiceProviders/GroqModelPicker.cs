using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using yuudachi.Groq;

namespace yuudachi.ChoiceProviders;

public class GroqModelPicker : IChoicesProvider<ApplicationCommandContext>
{
    public static GroqClient? Client {get;set;}
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