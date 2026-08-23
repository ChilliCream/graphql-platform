using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope every <c>nitro agent hook codex &lt;event&gt;</c>
/// stdin-based subcommand (<c>session-start</c>, <c>user-prompt-submit</c>,
/// <c>session-end</c>) runs its handler through - the Codex analog of
/// <see cref="ClaudeHookExecutor"/>, same contract: malformed payload,
/// database contention, a schema version mismatch, a missing workspace, any
/// exception a handler raises, and the timeout itself all resolve to the
/// same neutral <c>{}</c> response. The separate <c>notify</c> command is
/// NOT run through this: it reads its payload from argv, not stdin, and its
/// exit code (not just its stdout) has to carry the foreign-wrapping
/// contract, see <c>CodexNotifyHookCommand</c>.
/// </summary>
internal static class CodexHookExecutor
{
    /// <summary>
    /// Same failure ceiling as <see cref="ClaudeHookExecutor.EntryTimeout"/>:
    /// not a latency target, the point past which a hung handler must not be
    /// allowed to wedge Codex's turn any longer.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    public static Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        Func<CodexHookPayload, CancellationToken, Task<CodexHookOutcome>> handle,
        string hookEventName,
        CancellationToken cancellationToken)
        => RunAsync(environmentVariables, input, output, handle, hookEventName, EntryTimeout, cancellationToken);

    /// <summary>
    /// Overload taking an explicit <paramref name="timeout"/> instead of
    /// <see cref="EntryTimeout"/>, so a test can prove the timeout path
    /// fails open without waiting out the real entry timeout.
    /// </summary>
    internal static async Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        Func<CodexHookPayload, CancellationToken, Task<CodexHookOutcome>> handle,
        string hookEventName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed(environmentVariables))
        {
            await WriteAsync(output, CodexHookOutcome.Neutral, hookEventName, cancellationToken);
            return ExitCode;
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        var outcome = CodexHookOutcome.Neutral;

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
            // CodexHookOutcome.Neutral without awaiting `runTask` - a handler
            // ignoring cancellation must not be allowed to keep this call,
            // and Codex, waiting past the timeout.
        }
        catch
        {
            // Fail-open on EVERYTHING: an empty or malformed payload or a
            // handler exception (database contention, a schema version
            // mismatch). `outcome` is still CodexHookOutcome.Neutral, so
            // Codex always gets a valid neutral response, never an error.
            outcome = CodexHookOutcome.Neutral;
        }

        await WriteAsync(output, outcome, hookEventName, cancellationToken);

        return ExitCode;
    }

    private static async Task<CodexHookOutcome> RunHandlerAsync(
        TextReader input,
        Func<CodexHookPayload, CancellationToken, Task<CodexHookOutcome>> handle,
        CancellationToken cancellationToken)
    {
        var json = await input.ReadToEndAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize(json, CodexHookJsonContext.Default.CodexHookPayload);

        return payload is null ? CodexHookOutcome.Neutral : await handle(payload, cancellationToken);
    }

    // Always success: a hook adapter reports failure to Codex through its
    // own JSON protocol (or silently, via the neutral response), never
    // through the process exit code.
    private const int ExitCode = 0;

    private static bool IsSuppressed(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true";

    private static async Task WriteAsync(
        TextWriter output, CodexHookOutcome outcome, string hookEventName, CancellationToken cancellationToken)
    {
        var response = ToResponse(outcome, hookEventName);
        var json = JsonSerializer.Serialize(response, CodexHookJsonContext.Default.CodexHookResponse);

        await output.WriteAsync(json.AsMemory(), cancellationToken);
        await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
    }

    private static CodexHookResponse ToResponse(CodexHookOutcome outcome, string hookEventName)
    {
        if (outcome.AdditionalContext is { Length: > 0 } context)
        {
            return new CodexHookResponse
            {
                HookSpecificOutput = new CodexHookSpecificOutput
                {
                    HookEventName = hookEventName,
                    AdditionalContext = context
                }
            };
        }

        return new CodexHookResponse();
    }
}
