using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Never returns, so a caller racing it against its own timeout always
/// observes the timeout side of that race.
/// </summary>
internal sealed class NeverCompletingCodexQueueClient : ICodexQueueClient
{
    public async Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return CodexQueueResult.Ok;
    }
}
