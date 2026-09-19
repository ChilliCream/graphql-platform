using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope <c>nitro agent hook codex notify</c> runs through.
/// The exit code is part of the contract, not always success: this process finishes
/// with the wrapped foreign program's own exit code when one is configured. A
/// malformed payload, a handler exception, or the entry timeout still fall through
/// to attempting the foreign exec.
/// </summary>
internal static class CodexNotifyExecutor
{
    /// <summary>
    /// Same failure ceiling as <see cref="CodexHookExecutor.EntryTimeout"/>.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The exit code when no foreign program is configured: success.
    /// </summary>
    private const int NoForeignExitCode = 0;

    public static Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        Func<CodexNotifyPayload, CancellationToken, Task<CodexNotifyOutcome>> handleOurWork,
        Func<CancellationToken, Task<int?>> execForeign,
        string payloadJson,
        CancellationToken cancellationToken)
        => RunAsync(
            environmentVariables, handleOurWork, execForeign, payloadJson, EntryTimeout, cancellationToken);

    /// <summary>
    /// Overload taking an explicit <paramref name="timeout"/> instead of
    /// <see cref="EntryTimeout"/>.
    /// </summary>
    internal static async Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        Func<CodexNotifyPayload, CancellationToken, Task<CodexNotifyOutcome>> handleOurWork,
        Func<CancellationToken, Task<int?>> execForeign,
        string payloadJson,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!IsSuppressed(environmentVariables))
        {
            await RunOurWorkAsync(handleOurWork, payloadJson, timeout, cancellationToken);
        }

        // NITRO_HOOK_SUPPRESS only suppresses our own mail work; it must never
        // suppress the foreign program the operator configured.
        var foreignExitCode = await TryExecForeignAsync(execForeign, cancellationToken);

        return foreignExitCode ?? NoForeignExitCode;
    }

    private static async Task RunOurWorkAsync(
        Func<CodexNotifyPayload, CancellationToken, Task<CodexNotifyOutcome>> handleOurWork,
        string payloadJson,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        try
        {
            var payload = JsonSerializer.Deserialize(payloadJson, CodexHookJsonContext.Default.CodexNotifyPayload);

            if (payload is null)
            {
                return;
            }

            var runTask = handleOurWork(payload, linkedSource.Token);
            var timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeoutSource.Token);

            await Task.WhenAny(runTask, timeoutTask);

            // Either the handler finished, or the entry timeout won the race and
            // runTask is abandoned rather than awaited. The foreign exec below is
            // unconditional either way.
        }
        catch
        {
            // Fail-open on everything: malformed payload JSON or a handler
            // exception. The foreign exec below still runs regardless.
        }
    }

    private static async Task<int?> TryExecForeignAsync(
        Func<CancellationToken, Task<int?>> execForeign, CancellationToken cancellationToken)
    {
        try
        {
            return await execForeign(cancellationToken);
        }
        catch
        {
            // Fail-open: a spawn-time failure in the caller's own foreign-exec
            // delegate must not crash this process either.
            return null;
        }
    }

    private static bool IsSuppressed(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true";
}
