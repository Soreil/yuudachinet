using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using yuudachi;
using yuudachi.Deepseek;

namespace Tests;

public class DeepSeekUsageTests
{
    private string directory = null!;
    private string filePath = null!;
    private DeepSeekUsageStore store = null!;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "yuudachi-usage-tests-" + Guid.NewGuid().ToString("N"));
        filePath = Path.Combine(directory, "usage.json");
        store = CreateStore();
    }

    [TearDown]
    public void TearDown()
    {
        store.Dispose();
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    [Test]
    public async Task PersistsCountersAcrossStoreInstancesWithoutSavingConversationText()
    {
        await store.RecordAsync(new(1, 10), [new(ChatRole.User, "private question")], Response("private answer", 123, 45), failed: false);
        using var reloaded = CreateStore();
        var users = await reloaded.GetUsageAsync(new(1, 10));
        var json = await File.ReadAllTextAsync(filePath);

        Assert.Multiple(() =>
        {
            Assert.That(users.Single(), Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, InputTokens: 123, OutputTokens: 45)));
            Assert.That(json, Does.Not.Contain("private question").And.Not.Contain("private answer"));
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
        });
    }

    [Test]
    public async Task EstimatesMissingCountsUsingAllRequestMessages()
    {
        await store.RecordAsync(new(1, 10), [new(ChatRole.System, "rule"), new(ChatRole.User, "12345")],
            Response("123456789"), failed: false);
        var usage = (await store.GetUsageAsync(new(1, 10))).Single();

        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, InputTokens: 13, OutputTokens: 3, EstimatedCalls: 1)));
    }

    [Test]
    public async Task EstimatesOnlyMissingCountersAndPreservesReportedZero()
    {
        await store.RecordAsync(new(1, 10), [new(ChatRole.User, "12345")], Response("123456789", input: 0), failed: false);
        var usage = (await store.GetUsageAsync(new(1, 10))).Single();

        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, InputTokens: 0, OutputTokens: 3, EstimatedCalls: 1)));
    }

    [Test]
    public async Task FailedCallsWithoutResponsesDoNotInventBilledTokenCounts()
    {
        await store.RecordAsync(new(1, 10), [new(ChatRole.User, "12345")], null, failed: true);
        var usage = (await store.GetUsageAsync(new(1, 10))).Single();

        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, FailedCalls: 1, UnmeteredCalls: 1)));
    }

    [Test]
    public async Task ConcurrentCallsAreNotLost()
    {
        await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => store.RecordAsync(new(1, 10),
            [new(ChatRole.User, "question")], Response("answer", 10, 5), failed: false)));
        var usage = (await store.GetUsageAsync(new(1, 10))).Single();

        Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 25, InputTokens: 250, OutputTokens: 125)));
    }

    [Test]
    public async Task LeaderboardIsRankedByCallsThenTokensAndIsScopedToTheServer()
    {
        await store.RecordAsync(new(1, 10), [], Response("answer", 10, 5), failed: false);
        await store.RecordAsync(new(1, 10), [], Response("answer", 10, 5), failed: false);
        await store.RecordAsync(new(2, 10), [], Response("answer", 100, 50), failed: false);
        await store.RecordAsync(new(3, 10), [], Response("answer", 10, 5), failed: false);
        await store.RecordAsync(new(99, 20), [], Response("answer", 1000, 500), failed: false);
        await store.RecordAsync(new(98), [], Response("answer", 1000, 500), failed: false);

        var users = await store.GetUsageAsync(new(1, 10));

        Assert.That(users.Select(user => user.UserId), Is.EqualTo(new ulong[] { 1, 2, 3 }));
    }

    [Test]
    public async Task DirectMessagesOnlyExposeTheRequestingUsersOwnUsage()
    {
        await store.RecordAsync(new(1), [], Response("answer", 10, 5), failed: false);
        await store.RecordAsync(new(2), [], Response("answer", 100, 50), failed: false);
        await store.RecordAsync(new(1, 10), [], Response("answer", 1000, 500), failed: false);

        var users = await store.GetUsageAsync(new(1));

        Assert.That(users, Is.EqualTo(new[] { new DeepSeekUsage(1, null, Calls: 1, InputTokens: 10, OutputTokens: 5) }));
    }

    [TestCase("invalid json")]
    [TestCase("null")]
    [TestCase("[null]")]
    [TestCase("[{\"UserId\":1,\"Calls\":-1}]")]
    [TestCase("[{\"UserId\":1},{\"UserId\":1}]")]
    public async Task CorruptCountersAreNotSilentlyResetOrOverwritten(string content)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, content);

        var readFailure = await Assert.ThrowsAsync<DeepSeekUsageTrackingException>(async () => await store.GetUsageAsync(new(1)));
        await Assert.ThrowsAsync<DeepSeekUsageTrackingException>(async () =>
            await store.RecordAsync(new(1), [], Response("answer", 10, 5), failed: false));

        Assert.Multiple(() =>
        {
            Assert.That(readFailure!.InnerException, Is.InstanceOf<JsonException>());
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(content));
        });
    }

    [Test]
    public async Task FileSystemErrorsAreExplicitTrackingFailures()
    {
        Directory.CreateDirectory(filePath);
        await Assert.ThrowsAsync<DeepSeekUsageTrackingException>(async () =>
            await store.RecordAsync(new(1), [], Response("answer", 10, 5), failed: false));
        Assert.That(Directory.Exists(filePath), Is.True);
    }

    [Test]
    public async Task NewStoreHasNoUsage()
    {
        Assert.That(await store.GetUsageAsync(new(1, 10)), Is.Empty);
        Assert.That(File.Exists(filePath), Is.False);
    }

    [Test]
    public void ReportIncludesTopTenAndOwnUsageWithinDiscordEmbedLimits()
    {
        var users = Enumerable.Range(1, 15).Select(index => new DeepSeekUsage((ulong)index, 10,
            Calls: 16 - index, FailedCalls: 1, InputTokens: 100, OutputTokens: 25, EstimatedCalls: 1, UnmeteredCalls: 1)).ToArray();
        var report = DeepSeekUsageReport.Create(users, new(15, 10));
        var fields = report.Fields!.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.Description!.Split('\n'), Has.Length.EqualTo(10));
            Assert.That(report.Description, Does.Contain("<@1>").And.Contain("<@10>").And.Not.Contain("<@11>"));
            Assert.That(fields.Single(field => field.Name == "Recorded totals").Value, Does.Contain("Calls: 120 (15 failed)").And.Contain($"Total tokens: {1875:N0}"));
            Assert.That(fields.Single(field => field.Name == "Your usage").Value, Does.Contain("Calls: 1 (1 failed)").And.Contain("Responses with estimated tokens: 1"));
            Assert.That(report.Description.Length, Is.LessThanOrEqualTo(4096));
            Assert.That(fields.All(field => field.Value.Length <= 1024), Is.True);
            Assert.That(report.Footer!.Text, Does.Contain("estimate").And.Contain("history"));
        });
    }

    [Test]
    public void LargeCountersStillFitDiscordEmbedLimits()
    {
        const long largeCount = long.MaxValue / 100;
        var users = Enumerable.Range(1, 15).Select(index => new DeepSeekUsage(ulong.MaxValue - (ulong)index, 10,
            Calls: largeCount, FailedCalls: largeCount, InputTokens: largeCount, OutputTokens: largeCount,
            EstimatedCalls: largeCount, UnmeteredCalls: largeCount)).ToArray();
        var report = DeepSeekUsageReport.Create(users, new(1, 10));
        var fields = report.Fields!.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.Description!.Length, Is.LessThanOrEqualTo(4096));
            Assert.That(fields.All(field => field.Value.Length <= 1024), Is.True);
            Assert.That(report.Description.Length + fields.Sum(field => field.Name.Length + field.Value.Length)
                + report.Title!.Length + report.Footer!.Text.Length, Is.LessThanOrEqualTo(6000));
        });
    }

    [Test]
    public async Task PersistsSearchCountersSeparatelyFromModelTokens()
    {
        await store.RecordSearchAsync(new(1, 10), failed: false, billable: true);
        await store.RecordSearchAsync(new(1, 10), failed: false, billable: false);
        await store.RecordSearchAsync(new(1, 10), failed: true, billable: false);
        await store.RecordAsync(new(1, 10), [], Response("answer", 40, 10), failed: false);
        using var reloaded = CreateStore();
        var usage = (await reloaded.GetUsageAsync(new(1, 10))).Single();
        var report = DeepSeekUsageReport.Create([usage], new(1, 10));

        Assert.Multiple(() =>
        {
            Assert.That(usage, Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 1, InputTokens: 40, OutputTokens: 10,
                SearchCalls: 3, FailedSearchCalls: 1, BillableSearchCalls: 1)));
            Assert.That(usage.TotalCalls, Is.EqualTo(4));
            Assert.That(report.Fields!.First().Value, Does.Contain("Search calls: 3 (1 failed, 1 billable)").And.Contain("Total tokens: 50"));
            Assert.That(report.Footer!.Text, Does.Contain("Search is billed separately"));
        });
    }

    [Test]
    public async Task SearchCallsContributeToTheLeaderboard()
    {
        await store.RecordAsync(new(1, 10), [], Response("answer", 40, 10), failed: false);
        await store.RecordSearchAsync(new(2, 10), failed: false, billable: true);
        await store.RecordSearchAsync(new(2, 10), failed: false, billable: true);

        Assert.That((await store.GetUsageAsync(new(1, 10))).Select(user => user.UserId), Is.EqualTo(new ulong[] { 2, 1 }));
    }

    [Test]
    public async Task OldUsageFilesRemainReadableAfterAddingSearchCounters()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(filePath, "[{\"UserId\":1,\"GuildId\":10,\"Calls\":2,\"FailedCalls\":0,\"InputTokens\":100,\"OutputTokens\":20,\"EstimatedCalls\":0,\"UnmeteredCalls\":0}]");
        await store.RecordSearchAsync(new(1, 10), failed: false, billable: true);
        using var reloaded = CreateStore();

        Assert.That((await reloaded.GetUsageAsync(new(1, 10))).Single(),
            Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 2, InputTokens: 100, OutputTokens: 20, SearchCalls: 1, BillableSearchCalls: 1)));
    }

    [Test]
    public async Task ConcurrentSearchAndModelCallsDoNotOverwriteEachOther()
    {
        await Task.WhenAll(Enumerable.Range(0, 20).SelectMany(_ => new[]
        {
            store.RecordAsync(new(1, 10), [], Response("answer", 10, 5), failed: false),
            store.RecordSearchAsync(new(1, 10), failed: false, billable: true)
        }));

        Assert.That((await store.GetUsageAsync(new(1, 10))).Single(),
            Is.EqualTo(new DeepSeekUsage(1, 10, Calls: 20, InputTokens: 200, OutputTokens: 100, SearchCalls: 20, BillableSearchCalls: 20)));
    }

    private DeepSeekUsageStore CreateStore() => new(Options.Create(new DeepseekSettingsOptions { UsageFilePath = filePath }));

    private static ChatResponse Response(string text, long? input = null, long? output = null) => new(new ChatMessage(ChatRole.Assistant, text))
    {
        Usage = input is null && output is null ? null : new UsageDetails { InputTokenCount = input, OutputTokenCount = output }
    };
}