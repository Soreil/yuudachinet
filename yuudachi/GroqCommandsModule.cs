using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using yuudachi.Groq;

namespace yuudachi;

[SlashCommand("groq", "Groq commands")]
public class GroqCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly GroqClient groqClient;

    public GroqCommandsModule(GroqClient groqClient)
    {
        this.groqClient = groqClient;
    }

    [SubSlashCommand("modelinfo", "Lists model details")]
    public async Task<InteractionMessageProperties> GetGroqModels([SlashCommandParameter(ChoicesProviderType = typeof(GroqModelPicker))] string modelName)
    {
        var model = await groqClient.TryGetModel(modelName);
        if (model == null)
        {
            return "Model not found";
        }

        var res = new InteractionMessageProperties()
        {
            Embeds = [new EmbedProperties()
            {
                Title = model.Id,
                Description = model.OwnedBy,
                Fields = [
                    new EmbedFieldProperties(){
                    Name = "Context Window",
                    Value = model.ContextWindow.ToString(),
                },
                new EmbedFieldProperties()
                {
                    Name = "Released At",
                    Value = DateTimeOffset.FromUnixTimeSeconds( model.Created).ToString()
                }
            ]
            }]
        };

        return res;
    }
}