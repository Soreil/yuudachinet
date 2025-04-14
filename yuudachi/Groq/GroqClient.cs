namespace yuudachi.Groq;

internal class GroqClient
{
    private const string CompletionsRoot = @"https://api.groq.com/openai/v1/chat/completions";

    public HttpClient Client { get; }

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
}
