using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenAI;

using yuudachi.Commands;
using yuudachi.Deepseek;
using yuudachi;

namespace Tests;

public class DeepSeekTests
{
    private static readonly DeepSeekUsageIdentity Identity = new(1, 10);

    [Test]
    public async Task SendsChatToDeepSeekUsingConfiguredModelWithoutTools()
    {
        using var session = new ChatSession(Completion("Paris"));
        var result = await session.Client.ReplyAsync(new Conversation(), "What is the capital of France?", Identity);
        using var request = JsonDocument.Parse(session.Handler.Requests.Single());

        Assert.Multiple(() =>
        {
            Assert.That(session.Handler.Uris.Single(), Is.EqualTo(new Uri("https://api.deepseek.com/chat/completions")));
            Assert.That(request.RootElement.GetProperty("model").GetString(), Is.EqualTo("configured-deepseek-model"));
            Assert.That(request.RootElement.TryGetProperty("tools", out _), Is.False);
            Assert.That(request.RootElement.TryGetProperty("tool_choice", out _), Is.False);
            Assert.That(result.DiscordReply, Is.EqualTo("Paris"));
            Assert.That(result.Messages.Select(message => message.Role), Is.EqualTo(new[] { ChatRole.User, ChatRole.Assistant }));
        });
    }

    [Test]
    public async Task FollowUpsSendFullHistoryWithAssistantRoles()
    {
        using var session = new ChatSession(Completion("I will remember blue."), Completion("Your favourite colour is blue."), Completion("Still blue."));
        var first = await session.Client.ReplyAsync(new Conversation(), "My favourite colour is blue.", Identity);
        var second = await session.Client.ReplyAsync(first, "What is my favourite colour?", Identity);
        var third = await session.Client.ReplyAsync(second, "And now?", Identity);
        using var request = JsonDocument.Parse(session.Handler.Requests[2]);
        var messages = request.RootElement.GetProperty("messages").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(messages.Select(message => message.GetProperty("role").GetString()),
                Is.EqualTo(new[] { "user", "assistant", "user", "assistant", "user" }));
            Assert.That(messages.Select(message => message.GetProperty("content").GetString()),
                Is.EqualTo(new[] { "My favourite colour is blue.", "I will remember blue.", "What is my favourite colour?", "Your favourite colour is blue.", "And now?" }));
            Assert.That(third.Messages, Has.Count.EqualTo(6));
            Assert.That(third.DiscordReply, Is.EqualTo("Still blue."));
            Assert.That(first.Messages, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task SummaryUsesSystemPromptAndPreservesTemperatureForFollowUps()
    {
        using var session = new ChatSession(Completion("A short summary."), Completion("More detail."));
        var summary = await session.Client.ReplyAsync(new Conversation("Summarize this channel.", 0.8f), "Channel messages", Identity);
        await session.Client.ReplyAsync(summary, "Explain the summary.", Identity);
        using var request = JsonDocument.Parse(session.Handler.Requests[1]);
        var systemMessage = request.RootElement.GetProperty("messages")[0];

        Assert.Multiple(() =>
        {
            Assert.That(systemMessage.GetProperty("role").GetString(), Is.EqualTo("system"));
            Assert.That(systemMessage.GetProperty("content").GetString(), Is.EqualTo("Summarize this channel."));
            Assert.That(request.RootElement.GetProperty("temperature").GetSingle(), Is.EqualTo(0.8f));
        });
    }

    [Test]
    public async Task RepliesToTheSameMessageHaveIndependentHistories()
    {
        using var session = new ChatSession(Completion("Initial answer"), Completion("First branch answer"), Completion("Second branch answer"));
        var original = await session.Client.ReplyAsync(new Conversation(), "Initial question", Identity);
        var firstBranch = await session.Client.ReplyAsync(original, "First branch", Identity);
        var secondBranch = await session.Client.ReplyAsync(original, "Second branch", Identity);
        using var request = JsonDocument.Parse(session.Handler.Requests[2]);
        var messages = request.RootElement.GetProperty("messages").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(messages.Select(message => message.GetProperty("content").GetString()),
                Is.EqualTo(new[] { "Initial question", "Initial answer", "Second branch" }));
            Assert.That(original.Messages, Has.Count.EqualTo(2));
            Assert.That(firstBranch.DiscordReply, Is.EqualTo("First branch answer"));
            Assert.That(secondBranch.DiscordReply, Is.EqualTo("Second branch answer"));
        });
    }

    [Test]
    public async Task ApiFailureDoesNotPolluteConversationHistory()
    {
        using var session = new ChatSession(Completion("Initial answer"),
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":{\"message\":\"Invalid request\",\"type\":\"invalid_request_error\"}}", Encoding.UTF8, "application/json")
            }, Completion("Retry answer"));
        var original = await session.Client.ReplyAsync(new Conversation(), "Initial question", Identity);

        await Assert.ThrowsAsync<ClientResultException>(async () => await session.Client.ReplyAsync(original, "Failed question", Identity));
        var retried = await session.Client.ReplyAsync(original, "Retry question", Identity);
        using var request = JsonDocument.Parse(session.Handler.Requests[2]);

        Assert.Multiple(() =>
        {
            Assert.That(request.RootElement.GetProperty("messages").EnumerateArray()
                .Select(message => message.GetProperty("content").GetString()),
                Is.EqualTo(new[] { "Initial question", "Initial answer", "Retry question" }));
            Assert.That(retried.DiscordReply, Is.EqualTo("Retry answer"));
        });
        var usage = (await session.UsageStore.GetUsageAsync(Identity)).Single();
        Assert.Multiple(() =>
        {
            Assert.That(usage.Calls, Is.EqualTo(3));
            Assert.That(usage.FailedCalls, Is.EqualTo(1));
            Assert.That(usage.UnmeteredCalls, Is.EqualTo(1));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task EmptyAnswerIsAnExplicitFailure(string? answer)
    {
        using var session = new ChatSession(Completion(answer));
        var conversation = new Conversation();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await session.Client.ReplyAsync(conversation, "Hello", Identity));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo("No response received from DeepSeek."));
            Assert.That(conversation.Messages, Is.Empty);
        });
    }

    [TestCase(1999)]
    [TestCase(2000)]
    [TestCase(2001)]
    [TestCase(4000)]
    public async Task DiscordReplyRespectsLimitButHistoryKeepsFullAnswer(int length)
    {
        var answer = new string('a', length);
        using var session = new ChatSession(Completion(answer));
        var result = await session.Client.ReplyAsync(new Conversation(), "Hello", Identity);
        const string suffix = "... (truncated)";

        Assert.Multiple(() =>
        {
            Assert.That(result.DiscordReply.Length, Is.EqualTo(Math.Min(length, 2000)));
            Assert.That(result.DiscordReply, Is.EqualTo(length <= 2000 ? answer : answer[..(2000 - suffix.Length)] + suffix));
            Assert.That(result.Messages.Last().Text, Is.EqualTo(answer));
        });
    }

    [Test]
    public async Task CancellationIsPropagatedWithoutUpdatingHistory()
    {
        using var session = new ChatSession(Completion("Unused answer"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var conversation = new Conversation();

        await Assert.CatchAsync<OperationCanceledException>(async () => await session.Client.ReplyAsync(conversation, "Hello", Identity, cancellation.Token));
        Assert.That(conversation.Messages, Is.Empty);
        Assert.That(await session.UsageStore.GetUsageAsync(Identity), Is.Empty);
    }

    [Test]
    public void SlashCommandsDoNotExposeModelsOrTools()
    {
        var methods = typeof(DeepseekCommandsModule).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(NetCord.Services.ApplicationCommands.SubSlashCommandAttribute), false).Length != 0)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(methods.Select(method => method.Name), Is.EquivalentTo(new[] { "Ask", "Summarize", "Search", "Usage" }));
            Assert.That(methods.Single(method => method.Name == "Ask").GetParameters().Select(parameter => parameter.Name),
                Is.EqualTo(new[] { "question" }));
            Assert.That(methods.Single(method => method.Name == "Summarize").GetParameters().Select(parameter => parameter.Name),
                Is.EqualTo(new[] { "prompt", "numMessages", "temp" }));
            Assert.That(methods.Single(method => method.Name == "Search").GetParameters().Select(parameter => parameter.Name),
                Is.EqualTo(new[] { "query" }));
        });
    }

    [Test]
    public async Task ProviderUsageIsAttributedToTheUserMakingEachCall()
    {
        using var session = new ChatSession(Completion("First answer", 100, 20), Completion("Follow-up answer", 160, 30));
        var first = await session.Client.ReplyAsync(new Conversation(), "Question", Identity);
        await session.Client.ReplyAsync(first, "Follow-up from another user", new(2, 10));
        var users = await session.UsageStore.GetUsageAsync(Identity);

        Assert.Multiple(() =>
        {
            Assert.That(users.Select(user => user.UserId), Is.EqualTo(new ulong[] { 2, 1 }));
            Assert.That(users[0], Is.EqualTo(new DeepSeekUsage(2, 10, Calls: 1, InputTokens: 160, OutputTokens: 30)));
            Assert.That(users[1], Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, InputTokens: 100, OutputTokens: 20)));
        });
    }

    [Test]
    public async Task EmptyResponseStillRecordsBilledProviderTokens()
    {
        using var session = new ChatSession(Completion(null, 100, 50));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await session.Client.ReplyAsync(new Conversation(), "Question", Identity));
        var usage = (await session.UsageStore.GetUsageAsync(Identity)).Single();
        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, FailedCalls: 1, InputTokens: 100, OutputTokens: 50)));
    }

    [Test]
    public async Task MissingProviderUsageIsEstimatedIncludingFollowUpHistory()
    {
        using var session = new ChatSession(Completion("123456789"), Completion("end"));
        var first = await session.Client.ReplyAsync(new Conversation(), "12345", Identity);
        await session.Client.ReplyAsync(first, "abc", Identity);
        var usage = (await session.UsageStore.GetUsageAsync(Identity)).Single();

        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 2, InputTokens: 27, OutputTokens: 4, EstimatedCalls: 2)));
    }

    private static HttpResponseMessage Completion(string? content, long? inputTokens = null, long? outputTokens = null) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(new
        {
            id = "test-completion",
            @object = "chat.completion",
            created = 1700000000,
            model = "configured-deepseek-model",
            usage = inputTokens is null && outputTokens is null ? null : new
            {
                prompt_tokens = inputTokens,
                completion_tokens = outputTokens,
                total_tokens = (inputTokens ?? 0) + (outputTokens ?? 0)
            },
            choices = new[] { new { index = 0, message = new { role = "assistant", content }, finish_reason = "stop" } }
        }), Encoding.UTF8, "application/json")
    };

    private sealed class ChatSession : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly IChatClient chatClient;
        private readonly string usageDirectory = Path.Combine(Path.GetTempPath(), "yuudachi-usage-tests-" + Guid.NewGuid().ToString("N"));
        public RecordingHandler Handler { get; }
        public DeepSeekClient Client { get; }
        public DeepSeekUsageStore UsageStore { get; }

        public ChatSession(params HttpResponseMessage[] responses)
        {
            Handler = new RecordingHandler(responses);
            httpClient = new HttpClient(Handler);
            chatClient = new OpenAI.Chat.ChatClient("configured-deepseek-model", new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri("https://api.deepseek.com"),
                    Transport = new HttpClientPipelineTransport(httpClient),
                    RetryPolicy = new ClientRetryPolicy(0)
                }).AsIChatClient();
            UsageStore = new DeepSeekUsageStore(Options.Create(new DeepseekSettingsOptions
            {
                UsageFilePath = Path.Combine(usageDirectory, "usage.json")
            }));
            Client = new DeepSeekClient(chatClient, NullLogger<DeepSeekClient>.Instance, UsageStore);
        }

        public void Dispose()
        {
            chatClient.Dispose();
            httpClient.Dispose();
            Handler.Dispose();
            UsageStore.Dispose();
            if (Directory.Exists(usageDirectory))
                Directory.Delete(usageDirectory, recursive: true);
        }
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public List<string> Requests { get; } = [];
        public List<Uri?> Uris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Uris.Add(request.RequestUri);
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return responses.Dequeue();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var response in responses)
                    response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}