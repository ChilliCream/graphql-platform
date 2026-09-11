using System.Text.Json;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope every <c>nitro agent hook opencode &lt;event&gt;</c>
/// subcommand (<c>session-created</c>, <c>chat-message</c>,
/// <c>session-idle</c>, <c>session-deleted</c>) runs its handler through -
/// the opencode analog of <see cref="ClaudeHookExecutor"/> and
/// <see cref="CodexHookExecutor"/>, same contract: malformed payload,
/// database contention, a schema version mismatch, a missing workspace, any
/// exception a handler raises, and the timeout itself all resolve to the
/// same neutral <c>{}</c> response. The generated <c>nitro-hooks.js</c> shim
/// applies the response's <c>parts</c> to the current chat output; it never
/// sees anything else the handler returns. Unlike Claude and Codex,
/// opencode's response envelope carries no hook-event-name-keyed field
/// (there is no <c>hookSpecificOutput</c> equivalent), so this executor
/// takes no event name.
/// </summary>
internal static class OpencodeHookExecutor
{
    /// <summary>
    /// Same failure ceiling as <see cref="ClaudeHookExecutor.EntryTimeout"/>
    /// and <see cref="CodexHookExecutor.EntryTimeout"/>: not a latency
    /// target, the point past which a hung handler must not be allowed to
    /// wedge opencode's turn any longer.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    public static Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        TextWriter error,
        Func<OpencodeHookPayload, CancellationToken, Task<OpencodeHookOutcome>> handle,
        CancellationToken cancellationToken)
        => RunAsync(environmentVariables, input, output, error, handle, EntryTimeout, cancellationToken);

    /// <summary>
    /// Overload taking an explicit <paramref name="timeout"/> instead of
    /// <see cref="EntryTimeout"/>, so a test can prove the timeout path
    /// fails open without waiting out the real entry timeout.
    /// </summary>
    internal static async Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        TextWriter error,
        Func<OpencodeHookPayload, CancellationToken, Task<OpencodeHookOutcome>> handle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed(environmentVariables))
        {
            await WriteAsync(output, OpencodeHookOutcome.Neutral, cancellationToken);
            return ExitCode;
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        var outcome = OpencodeHookOutcome.Neutral;

        try
        {
            var runTask = RunHandlerAsync(input, handle, linkedSource.Token);
            var timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, timeoutSource.Token);

            var completed = await Task.WhenAny(runTask, timeoutTask);

            if (completed == runTask)
            {
                outcome = await runTask;
            }

            // Else: the entry timeout won the race. `outcome` stays
            // OpencodeHookOutcome.Neutral without awaiting `runTask` - a
            // handler ignoring cancellation must not be allowed to keep
            // this call, and opencode, waiting past the timeout.
        }
        catch (AgentWorkspaceSchemaMismatchException exception)
        {
            // Reported rather than swallowed, the same as the Claude and
            // Codex adapters: a stale schema keeps every hook of every
            // session inert until someone migrates it, and nothing else
            // ever says so.
            await error.WriteLineAsync(exception.Message.AsMemory(), cancellationToken);
            await WriteAsync(output, OpencodeHookOutcome.Neutral, cancellationToken);

            return FailureExitCode;
        }
        catch
        {
            // Fail-open on EVERYTHING else: an empty or malformed payload or
            // a handler exception (database contention, for example).
            // `outcome` is still OpencodeHookOutcome.Neutral, so opencode
            // always gets a valid neutral response, never an error.
            outcome = OpencodeHookOutcome.Neutral;
        }

        await WriteAsync(output, outcome, cancellationToken);

        return ExitCode;
    }

    private static async Task<OpencodeHookOutcome> RunHandlerAsync(
        TextReader input,
        Func<OpencodeHookPayload, CancellationToken, Task<OpencodeHookOutcome>> handle,
        CancellationToken cancellationToken)
    {
        var json = await input.ReadToEndAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize(json, OpencodeHookJsonContext.Default.OpencodeHookPayload);

        return payload is null ? OpencodeHookOutcome.Neutral : await handle(payload, cancellationToken);
    }

    // Always success: a hook adapter reports failure to opencode through its
    // own JSON protocol (or silently, via the neutral response), never
    // through the process exit code.
    private const int ExitCode = 0;

    /// <summary>
    /// The nonzero exit a hook uses to report a condition the user has to
    /// act on, mirroring <see cref="ClaudeHookExecutor"/> and
    /// <see cref="CodexHookExecutor"/>.
    /// </summary>
    private const int FailureExitCode = 1;

    private static bool IsSuppressed(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true";

    private static async Task WriteAsync(
        TextWriter output, OpencodeHookOutcome outcome, CancellationToken cancellationToken)
    {
        var response = ToResponse(outcome);
        var json = JsonSerializer.Serialize(response, OpencodeHookJsonContext.Default.OpencodeHookResponse);

        await output.WriteAsync(json.AsMemory(), cancellationToken);
        await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
    }

    private static OpencodeHookResponse ToResponse(OpencodeHookOutcome outcome)
        => outcome.Parts.Count == 0
            ? new OpencodeHookResponse()
            : new OpencodeHookResponse { Parts = outcome.Parts };
}
