using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope <c>nitro agent hook codex notify</c> runs through:
/// argv-based (not stdin, unlike every other member of this namespace, spike
/// S2), and its exit code IS part of the contract rather than always
/// success, because the install-flow's foreign-wrapping guarantee
/// ("preserving argv/stdin/cwd/exit code") requires this process to finish
/// with the WRAPPED foreign program's own exit code when one is configured.
/// Our own work never blocks the foreign program from running: a malformed
/// payload, a handler exception, or the entry timeout all still fall through
/// to attempting the foreign exec.
/// </summary>
internal static class CodexNotifyExecutor
{
    /// <summary>
    /// Same failure ceiling as <see cref="CodexHookExecutor.EntryTimeout"/>.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The exit code when no foreign program is configured (nothing to
    /// preserve an exit code FROM): success, the same fail-open contract
    /// every other adapter in this namespace uses.
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
    /// Overload taking an explicit <paramref name="timeout"/> so a test can
    /// prove the timeout path still reaches the foreign exec without waiting
    /// out the real entry timeout.
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

        // Suppressed or not, our own no-loop reentrancy guard
        // (NITRO_HOOK_SUPPRESS) is only about OUR mail work re-entering
        // through a spawned relay - it must never suppress the foreign
        // program the operator originally configured.
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

            // Either the handler finished (its outcome is not otherwise
            // consumed here - the foreign exec below is unconditional) or
            // the entry timeout won the race, in which case `runTask` is
            // deliberately abandoned rather than awaited, same reasoning as
            // CodexHookExecutor: a handler ignoring cancellation must not be
            // allowed to keep this call past the deadline.
        }
        catch
        {
            // Fail-open on EVERYTHING: malformed payload JSON or a handler
            // exception (database contention, a schema version mismatch).
            // The foreign exec below still runs regardless.
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
