using System.Text.Json;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace yuudachi.Deepseek;

public readonly record struct DeepSeekUsageIdentity(ulong UserId, ulong? GuildId = null);

public sealed record DeepSeekUsage(
    ulong UserId,
    ulong? GuildId,
    long Calls = 0,
    long FailedCalls = 0,
    long InputTokens = 0,
    long OutputTokens = 0,
    long EstimatedCalls = 0,
    long UnmeteredCalls = 0,
    long SearchCalls = 0,
    long FailedSearchCalls = 0,
    long BillableSearchCalls = 0)
{
    public long TotalTokens => checked(InputTokens + OutputTokens);
    public long TotalCalls => checked(Calls + SearchCalls);
    public long TotalFailedCalls => checked(FailedCalls + FailedSearchCalls);
}

public sealed class DeepSeekUsageTrackingException(string message, Exception innerException) : Exception(message, innerException);

public sealed class DeepSeekUsageStore(IOptions<DeepseekSettingsOptions> options) : IDisposable
{
    private readonly string filePath = options.Value.UsageFilePath;
    private readonly SemaphoreSlim gate = new(1, 1);

    public Task RecordAsync(DeepSeekUsageIdentity identity, IReadOnlyList<ChatMessage> messages, ChatResponse? response, bool failed)
    {
        return UpdateAsync(identity, current =>
        {
            var estimated = response is not null && (response.Usage?.InputTokenCount is null || response.Usage?.OutputTokenCount is null);
            var input = response is null ? 0 : response.Usage?.InputTokenCount
                ?? checked((messages.Sum(message => (long)message.Text.Length) + 3) / 4 + messages.Count * 4L + 2);
            var output = response is null ? 0 : response.Usage?.OutputTokenCount ?? ((long)response.Text.Length + 3) / 4;
            if (input < 0 || output < 0)
                throw new InvalidDataException("DeepSeek returned negative token counts.");

            return current with
            {
                Calls = checked(current.Calls + 1),
                FailedCalls = checked(current.FailedCalls + (failed ? 1 : 0)),
                InputTokens = checked(current.InputTokens + input),
                OutputTokens = checked(current.OutputTokens + output),
                EstimatedCalls = checked(current.EstimatedCalls + (estimated ? 1 : 0)),
                UnmeteredCalls = checked(current.UnmeteredCalls + (response is null ? 1 : 0))
            };
        });
    }

    public Task RecordSearchAsync(DeepSeekUsageIdentity identity, bool failed, bool billable)
    {
        return UpdateAsync(identity, current => current with
        {
            SearchCalls = checked(current.SearchCalls + 1),
            FailedSearchCalls = checked(current.FailedSearchCalls + (failed ? 1 : 0)),
            BillableSearchCalls = checked(current.BillableSearchCalls + (billable ? 1 : 0))
        });
    }

    private async Task UpdateAsync(DeepSeekUsageIdentity identity, Func<DeepSeekUsage, DeepSeekUsage> update)
    {
        await gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            var index = users.FindIndex(user => user.UserId == identity.UserId && user.GuildId == identity.GuildId);
            var current = index < 0 ? new DeepSeekUsage(identity.UserId, identity.GuildId) : users[index];
            var updated = update(current);
            if (index < 0)
                users.Add(updated);
            else
                users[index] = updated;

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(stream, users);
                    stream.Flush(flushToDisk: true);
                }
                File.Move(temporaryPath, filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or OverflowException)
        {
            throw new DeepSeekUsageTrackingException("Could not save DeepSeek usage counters.", ex);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<DeepSeekUsage>> GetUsageAsync(DeepSeekUsageIdentity viewer)
    {
        await gate.WaitAsync();
        try
        {
            var users = await ReadAsync();
            return users.Where(user => user.GuildId == viewer.GuildId && (viewer.GuildId is not null || user.UserId == viewer.UserId))
                .OrderByDescending(user => user.TotalCalls)
                .ThenByDescending(user => user.TotalTokens)
                .ThenBy(user => user.UserId)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or OverflowException)
        {
            throw new DeepSeekUsageTrackingException("Could not read DeepSeek usage counters.", ex);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<DeepSeekUsage>> ReadAsync()
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var users = await JsonSerializer.DeserializeAsync<List<DeepSeekUsage>>(stream)
                ?? throw new JsonException("The usage file must contain a list of counters.");
            if (users.Any(user => user is null || user.UserId == 0 || user.Calls < 0 || user.FailedCalls < 0 || user.FailedCalls > user.Calls
                || user.InputTokens < 0 || user.OutputTokens < 0 || user.EstimatedCalls < 0 || user.EstimatedCalls > user.Calls
                || user.UnmeteredCalls < 0 || user.UnmeteredCalls > user.FailedCalls
                || user.SearchCalls < 0 || user.FailedSearchCalls < 0 || user.FailedSearchCalls > user.SearchCalls
                || user.BillableSearchCalls < 0 || user.BillableSearchCalls > user.SearchCalls)
                || users.GroupBy(user => (user.UserId, user.GuildId)).Any(group => group.Count() > 1))
                throw new JsonException("The usage file contains invalid or duplicate counters.");
            return users;
        }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
    }

    public void Dispose() => gate.Dispose();
}