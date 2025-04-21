using Microsoft.Extensions.Options;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using yuudachi.ChoiceProviders;
using yuudachi.Groq;

namespace yuudachi.Commands;

[SlashCommand("groq", "Groq commands")]
public class GroqCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly GroqClient groqClient;
    private string MostRecentModelName;

    public GroqCommandsModule(GroqClient groqClient, IOptions<GroqSettingsOptions> settings)
    {
        this.groqClient = groqClient;
        MostRecentModelName = settings.Value.DefaultModelName;
    }

    [SubSlashCommand("ask", "Ask a question")]
    public async Task<InteractionMessageProperties> Ask(
        [SlashCommandParameter(Description = "Your very interesting query", MinLength = 1)] string question,
        [SlashCommandParameter(Description = "Temperature controls how creative the answers are, 0.0 is rigid and 2.0 is extremely cooked", MinValue = 0.0, MaxValue = 2.0)] double temp = 0.6,
        [SlashCommandParameter(ChoicesProviderType = typeof(GroqModelPicker))] string? modelName = null)
    {
        if (modelName is null)
        {
            modelName = MostRecentModelName;
        }
        else
        {
            MostRecentModelName = modelName;
        }

        var model = await groqClient.TryGetModel(modelName);
        if (model == null || model.Id is null)
        {
            return "Model not found";
        }
        var convo = GroqClient.StartConversation(model.Id, startingTemperature: temp);
        convo.AddMessage(question);
        var res = await groqClient.ConversationResult(convo);

        var result = new InteractionMessageProperties()
        {
            Content = res.Choices[0].Message.Content,

            Components = [new ActionRowProperties(
                    [
                    new ButtonProperties("ReplyToCommand",
                    "Reply",
                    ButtonStyle.Primary)
                    ])
            ]
        };

        return result;
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