using System.Text.Json;

namespace yuudachi.Groq;

public class GroqClient
{
    private const string CompletionsRoot = @"https://api.groq.com/openai/v1/chat/completions";

    private HttpClient Client { get; }

    public GroqClient(string authToken)
    {
        Client = new HttpClient()
        {
            BaseAddress = new Uri(CompletionsRoot),
            DefaultRequestHeaders =
            {
                { "User-Agent", "Yuudachi" },
                { "Accept", "application/json" },
                { "Authorization",  $"Bearer {authToken}"}
            }
        };
    }

    public async Task<GroqResponse> ConversationResult(Conversation conversation, CancellationToken ct = default)
    {
        var request = conversation.ToGroqRequest();

        var json = JsonSerializer.Serialize(request);

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(CompletionsRoot, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase}");
        }
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        var result = JsonSerializer.Deserialize<GroqResponse>(responseContent);

        return result ?? throw new Exception("Failed to deserialize response");
    }

    public static Conversation StartConversation(string model, string? prompt = "", double startingTemperature = 0.6)
    {
        var convo = new Conversation(model, startingTemperature);
        if (!string.IsNullOrEmpty(prompt))
            convo.SetSystemMessage(Message.NewSystemMessage(prompt));
        return convo;
    }
}
