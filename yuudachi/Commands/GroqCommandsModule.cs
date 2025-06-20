using Microsoft.Extensions.Logging;
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
    private readonly GroqConversationHistory groqConversationHistory;
    private readonly ILogger<GroqCommandsModule> logger;
    private string MostRecentModelName;
    private const string errorPrefix = "Oopsie woopsie we got an error groq sisters: ";

    public GroqCommandsModule(GroqClient groqClient, GroqConversationHistory groqConversationHistory, IOptions<GroqSettingsOptions> settings, ILogger<GroqCommandsModule> logger)
    {
        this.groqClient = groqClient;
        this.groqConversationHistory = groqConversationHistory;
        this.logger = logger;
        MostRecentModelName = settings.Value.DefaultModelName;
    }

    [SubSlashCommand("ask", "Ask a question")]
    public async Task Ask(
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
            logger.LogWarning("Model not found: {ModelName}", modelName);
            _ = await RespondAsync(InteractionCallback.Message($"{errorPrefix}Model not found"), false);
            return;
        }

        var convo = GroqClient.StartConversation(model.Id, startingTemperature: temp);
        convo.AddMessage(question);

        GroqResponse? response;
        var loadingResponse = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading), true);
        try
        {
            response = await groqClient.ConversationResult(convo);
            if (response is null || response.Choices.Count == 0)
            {
                logger.LogWarning("No response received from Groq for question: {Question}", question);
                _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}No response received from Groq"));
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while calling Groq API");
            _ = await ModifyResponseAsync(x => x.WithContent($"{errorPrefix}{ex.Message}"));
            return;
        }

        var truncationWarning = "... (truncated)";
        var maxSize = 2000 - truncationWarning.Length;

        string content = response.Choices[0].Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("Received empty response from Groq for question: {Question}", question);
            content = $"{errorPrefix}No response received from Groq";
        }


        else if (content.Length > maxSize)
        {
            logger.LogWarning("Response content too long, truncating for question: {Question}", question);
            content = content[..maxSize] + truncationWarning;
        }

        //var result = new InteractionMessageProperties()
        //{
        //    Content = content,
        //};

        var r = await ModifyResponseAsync(x => x.WithContent(content));
        //var reply = await RespondAsync(InteractionCallback.Message(result), true);

        if (loadingResponse?.Interaction.ResponseMessageId is not null)
        {
            groqConversationHistory.Conversations.Add(loadingResponse.Interaction.ResponseMessageId.Value, convo);
        }
        else
        {
            logger.LogWarning("Failed to get response message ID for Groq conversation");
        }
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