using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Injects a digest into a Codex thread via <c>codex queue --thread &lt;id&gt; --message &lt;text&gt;</c>
/// (spike S2, perles-net-k3j.2): a durable,
/// cross-process write into <c>~/.codex/queue_1.sqlite</c>, decoupled from
/// any live Codex process. The message is delivered as an extra prepended
/// user-message item ahead of whatever the thread's next actual turn is,
/// sharing that turn's single <c>notify</c> firing.
/// </summary>
internal interface ICodexQueueClient
{
    /// <summary>
    /// Returns true when the <c>codex queue</c> subprocess exited zero.
    /// Never throws: a spawn failure, a nonzero exit, or a timeout all
    /// return false - fail-open, matching every other adapter member in this
    /// namespace. The ledger reservation this call's caller already made
    /// stands regardless (the plan's documented reserve-then-emit crash
    /// policy: a queue call lost after reservation suppresses that message
    /// on the gate channel, never a duplicate).
    /// </summary>
    Task<bool> QueueAsync(string threadId, string message, CancellationToken cancellationToken);
}

internal sealed class CodexQueueClient : ICodexQueueClient
{
    /// <summary>
    /// Bounds the subprocess: spike S2 measured a live call at ~300-360ms,
    /// dominated by process spawn. A generous multiple of that, well inside
    /// the 10s hook/notify entry timeout this call's caller enforces.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<bool> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("codex")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add("queue");
            startInfo.ArgumentList.Add("--thread");
            startInfo.ArgumentList.Add(threadId);
            startInfo.ArgumentList.Add("--message");
            startInfo.ArgumentList.Add(message);

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            using var timeoutSource = new CancellationTokenSource(Timeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedSource.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            // Fail-open: no "codex" on PATH, permission denied, or any other
            // spawn-time failure.
            return false;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Already exited between the timeout and this call.
        }
    }
}
