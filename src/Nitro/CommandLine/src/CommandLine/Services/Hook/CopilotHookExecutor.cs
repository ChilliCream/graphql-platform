using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope every <c>nitro agent hook copilot &lt;event&gt;</c>
/// subcommand (<c>session-start</c>, <c>user-prompt-submit</c>,
/// <c>session-end</c>) runs its handler through - the Copilot analog of
/// <see cref="ClaudeHookExecutor"/>/<see cref="CodexHookExecutor"/>, same
/// contract: malformed payload, database contention, a schema version
/// mismatch, a missing workspace, any exception a handler raises, and the
/// timeout itself all resolve to the same neutral <c>{}</c> response. Unlike
/// the other two harnesses, Copilot's response envelope carries no event
/// name (see <see cref="CopilotHookResponse"/>), so this executor's contract
/// needs no <c>hookEventName</c> parameter.
/// </summary>
internal static class CopilotHookExecutor
{
    /// <summary>
    /// Same failure ceiling as <see cref="ClaudeHookExecutor.EntryTimeout"/>:
    /// not a latency target, the point past which a hung handler must not be
    /// allowed to wedge Copilot's turn any longer.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    public static Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        Func<CopilotHookPayload, CancellationToken, Task<CopilotHookOutcome>> handle,
        CancellationToken cancellationToken)
        => RunAsync(environmentVariables, input, output, handle, EntryTimeout, cancellationToken);

    /// <summary>
    /// Overload taking an explicit <paramref name="timeout"/> instead of
    /// <see cref="EntryTimeout"/>, so a test can prove the timeout path
    /// fails open without waiting out the real entry timeout.
    /// </summary>
    internal static async Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        Func<CopilotHookPayload, CancellationToken, Task<CopilotHookOutcome>> handle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed(environmentVariables))
        {
            await WriteAsync(output, CopilotHookOutcome.Neutral, cancellationToken);
            return ExitCode;
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        var outcome = CopilotHookOutcome.Neutral;

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
            // CopilotHookOutcome.Neutral without awaiting `runTask` - a
            // handler ignoring cancellation must not be allowed to keep
            // this call, and Copilot, waiting past the timeout.
        }
        catch
        {
            // Fail-open on EVERYTHING: an empty or malformed payload or a
            // handler exception (database contention, a schema version
            // mismatch). `outcome` is still CopilotHookOutcome.Neutral, so
            // Copilot always gets a valid neutral response, never an error.
            outcome = CopilotHookOutcome.Neutral;
        }

        await WriteAsync(output, outcome, cancellationToken);

        return ExitCode;
    }

    private static async Task<CopilotHookOutcome> RunHandlerAsync(
        TextReader input,
        Func<CopilotHookPayload, CancellationToken, Task<CopilotHookOutcome>> handle,
        CancellationToken cancellationToken)
    {
        var json = await input.ReadToEndAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize(json, CopilotHookJsonContext.Default.CopilotHookPayload);

        return payload is null ? CopilotHookOutcome.Neutral : await handle(payload, cancellationToken);
    }

    // Always success: a hook adapter reports failure to Copilot through its
    // own JSON protocol (or silently, via the neutral response), never
    // through the process exit code.
    private const int ExitCode = 0;

    private static bool IsSuppressed(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true";

    private static async Task WriteAsync(
        TextWriter output, CopilotHookOutcome outcome, CancellationToken cancellationToken)
    {
        var response = ToResponse(outcome);
        var json = JsonSerializer.Serialize(response, CopilotHookJsonContext.Default.CopilotHookResponse);

        await output.WriteAsync(json.AsMemory(), cancellationToken);
        await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
    }

    private static CopilotHookResponse ToResponse(CopilotHookOutcome outcome)
        => outcome.AdditionalContext is { Length: > 0 } context
            ? new CopilotHookResponse { AdditionalContext = context }
            : new CopilotHookResponse();
}
