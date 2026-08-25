using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a <c>codex queue</c> call resolved to.
/// </summary>
internal enum CodexQueueResult
{
    /// <summary>
    /// The subprocess exited zero: the message was durably queued.
    /// </summary>
    Ok,

    /// <summary>
    /// The subprocess reported that the thread no longer exists.
    /// </summary>
    EndpointGone,

    /// <summary>
    /// A spawn failure, a timeout, or any other nonzero exit that is not the
    /// gone-thread signature.
    /// </summary>
    Error
}

/// <summary>
/// Injects a digest into a Codex thread via
/// <c>codex queue --thread &lt;id&gt; --message &lt;text&gt;</c>, a durable
/// cross-process write decoupled from
/// any live Codex process. The message is delivered as an extra prepended
/// user-message item ahead of whatever the thread's next actual turn is,
/// sharing that turn's single <c>notify</c> firing.
/// </summary>
internal interface ICodexQueueClient
{
    /// <summary>
    /// Runs the <c>codex queue</c> subprocess and classifies its outcome.
    /// Never throws: a spawn failure, a nonzero exit, or a timeout all
    /// return <see cref="CodexQueueResult.Error"/> (or
    /// <see cref="CodexQueueResult.EndpointGone"/> for the gone-thread
    /// signature) - fail-open, matching every other adapter member in this
    /// namespace. The ledger reservation this call's caller already made
    /// stands regardless (the plan's documented reserve-then-emit crash
    /// policy: a queue call lost after reservation suppresses that message
    /// on the gate channel, never a duplicate).
    /// </summary>
    Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken);
}

internal sealed class CodexQueueClient : ICodexQueueClient
{
    /// <summary>
    /// Bounds the subprocess well inside the hook/notify entry timeout
    /// enforced by the caller.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = BuildStartInfo(threadId, message);

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return CodexQueueResult.Error;
            }

            using var timeoutSource = new CancellationTokenSource(Timeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutSource.Token);

            string stderr;

            try
            {
                // Read stderr concurrently with waiting for exit: the pipe's
                // buffer is bounded, so a caller that waits for exit first
                // and reads after can deadlock against a child that blocks
                // writing a large enough error message.
                var stderrTask = process.StandardError.ReadToEndAsync(linkedSource.Token);
                await process.WaitForExitAsync(linkedSource.Token);
                stderr = await stderrTask;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return CodexQueueResult.Error;
            }

            return MapResult(process.ExitCode, stderr);
        }
        catch
        {
            // Fail-open: no "codex" on PATH, permission denied, or any other
            // spawn-time failure.
            return CodexQueueResult.Error;
        }
    }

    /// <summary>
    /// Classifies a completed <c>codex queue</c> invocation. Internal, not
    /// private: <c>CodexQueueClientTests</c> exercises this directly against
    /// stderr fixtures without shelling out to a real <c>codex</c> binary.
    /// </summary>
    internal static CodexQueueResult MapResult(int exitCode, string stderr)
    {
        if (exitCode == 0)
        {
            return CodexQueueResult.Ok;
        }

        if (stderr.Contains("no rollout found for thread id", StringComparison.Ordinal)
            || stderr.Contains("No active session found matching", StringComparison.Ordinal))
        {
            return CodexQueueResult.EndpointGone;
        }

        return CodexQueueResult.Error;
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

    // Internal, not private: CodexQueueClientTests asserts the built
    // ProcessStartInfo's environment countermeasures directly.
    internal static ProcessStartInfo BuildStartInfo(string threadId, string message)
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

        startInfo.Environment.Remove("NITRO_MAIL_ACTOR");
        startInfo.Environment.Remove("NITRO_TASK_ACTOR");
        startInfo.Environment["NITRO_HOOK_SUPPRESS"] = "1";

        return startInfo;
    }
}
