using Microsoft.Extensions.Options;

using System.Text.Json;

namespace yuudachi.Groq;

public class GroqClient
{
    private const string APIRoot = @"https://api.groq.com/openai/v1/";
    private const string Completions = @"chat/completions";
    private const string Models = @"models";

    private HttpClient Client { get; }

    public GroqClient(string token)
    {
        Client = new HttpClient()
        {
            BaseAddress = new Uri(APIRoot),
            DefaultRequestHeaders =
            {
                { "User-Agent", "Yuudachi" },
                { "Accept", "application/json" },
                { "Authorization",  $"Bearer {token}"}
            }
        };
    }
    public GroqClient(IOptions<GroqClientKey> authToken) : this(authToken.Value.Key)
    {
    }

    public async Task<GroqModels> GetAllModels(CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync(Models, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GroqModels>(responseContent);
        return result ?? throw new Exception("Failed to deserialize response");
    }

    public async Task<GroqModelDescriptor?> TryGetModel(string name, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"models/{name}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GroqModelDescriptor>(responseContent);
        return result ?? throw new Exception("Failed to deserialize response");
    }

    public async Task<GroqResponse> ConversationResult(Conversation conversation, CancellationToken ct = default)
    {
        var request = conversation.ToGroqRequest();

        var json = JsonSerializer.Serialize(request);

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(Completions, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        var result = JsonSerializer.Deserialize<GroqResponse>(responseContent) ?? throw new Exception("Failed to deserialize response");

        var msg = Message.NewSystemMessage(result.Choices[0].Message.Content);
        conversation.AddResponse(msg);

        return result;
    }

    public static Conversation StartConversation(string model, string? prompt = "", double startingTemperature = 0.6)
    {
        var convo = new Conversation(model, startingTemperature);
        if (!string.IsNullOrEmpty(prompt))
            convo.SetSystemMessage(Message.NewSystemMessage(prompt));
        return convo;
    }
}
