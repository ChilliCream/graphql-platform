using System.Diagnostics;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexQueueClient : ICodexQueueClient
{
    /// <summary>
    /// The timeout for waiting for queue completion and reading its error output.
    /// </summary>
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

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

            using var timeoutSource = new CancellationTokenSource(s_timeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutSource.Token);

            string stderr;

            try
            {
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
            return CodexQueueResult.Error;
        }
    }

    /// <summary>
    /// Classifies a completed <c>codex queue</c> invocation.
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
            // Process termination is best effort.
        }
    }

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

        startInfo.Environment["NITRO_HOOK_SUPPRESS"] = "1";

        return startInfo;
    }
}
