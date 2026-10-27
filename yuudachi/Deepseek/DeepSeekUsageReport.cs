using NetCord.Rest;

namespace yuudachi.Deepseek;

public static class DeepSeekUsageReport
{
    public static EmbedProperties Create(IReadOnlyList<DeepSeekUsage> users, DeepSeekUsageIdentity viewer)
    {
        var ownUsage = users.SingleOrDefault(user => user.UserId == viewer.UserId) ?? new DeepSeekUsage(viewer.UserId, viewer.GuildId);
        var totals = new DeepSeekUsage(viewer.UserId, viewer.GuildId,
            users.Sum(user => user.Calls), users.Sum(user => user.FailedCalls),
            users.Sum(user => user.InputTokens), users.Sum(user => user.OutputTokens),
            users.Sum(user => user.EstimatedCalls), users.Sum(user => user.UnmeteredCalls),
            users.Sum(user => user.SearchCalls), users.Sum(user => user.FailedSearchCalls), users.Sum(user => user.BillableSearchCalls));
        List<EmbedFieldProperties> fields = [new() { Name = "Recorded totals", Value = Format(totals) }];
        if (viewer.GuildId is not null)
            fields.Add(new() { Name = "Your usage", Value = Format(ownUsage) });

        return new EmbedProperties
        {
            Title = viewer.GuildId is null ? "Your DeepSeek usage" : "DeepSeek usage - this server",
            Description = users.Count == 0 ? "No DeepSeek calls recorded yet." : string.Join('\n', users.Take(10).Select((user, index) =>
                $"**{index + 1}.** <@{user.UserId}>: **{user.TotalCalls:N0} calls**, {user.TotalTokens:N0} tokens ({user.TotalFailedCalls:N0} failed calls)")),
            Fields = fields,
            Footer = new EmbedFooterProperties
            {
                Text = "All-time since tracking began. API token counts where available; missing counts use a rough 4 characters/token estimate plus input overhead. Includes conversation history. Failures without a response have no token count. Calls include failures, not SDK retries. Search is billed separately per non-empty successful request, not tokens."
            }
        };
    }

    private static string Format(DeepSeekUsage usage) =>
        $"Calls: {usage.TotalCalls:N0} ({usage.TotalFailedCalls:N0} failed)\nModel calls: {usage.Calls:N0} ({usage.FailedCalls:N0} failed)\nSearch calls: {usage.SearchCalls:N0} ({usage.FailedSearchCalls:N0} failed, {usage.BillableSearchCalls:N0} billable)\nInput tokens: {usage.InputTokens:N0}\nOutput tokens: {usage.OutputTokens:N0}\nTotal tokens: {usage.TotalTokens:N0}\nResponses with estimated tokens: {usage.EstimatedCalls:N0}\nFailures without token counts: {usage.UnmeteredCalls:N0}";
}