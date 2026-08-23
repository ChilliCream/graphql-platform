using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope every <c>nitro agent hook claude &lt;event&gt;</c>
/// subcommand runs its handler through: reads the payload from stdin,
/// enforces the entry timeout, and writes a harness-shaped JSON response to
/// stdout. Malformed payload, database contention, a schema version
/// mismatch, a missing workspace, any exception a handler raises, and the
/// timeout itself all resolve to the same neutral <c>{}</c> response;
/// installing a hook must never be able to fail Claude's turn. Free of
/// System.CommandLine and DI types so it can run against a bare
/// <see cref="TextReader"/> and <see cref="TextWriter"/> in tests.
/// </summary>
internal static class ClaudeHookExecutor
{
    /// <summary>
    /// The failure ceiling named in the plan: not a latency target, the
    /// point past which a hung handler must not be allowed to wedge the
    /// harness's turn any longer.
    /// </summary>
    public static readonly TimeSpan EntryTimeout = TimeSpan.FromSeconds(10);

    public static Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        Func<ClaudeHookPayload, CancellationToken, Task<ClaudeHookOutcome>> handle,
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
        Func<ClaudeHookPayload, CancellationToken, Task<ClaudeHookOutcome>> handle,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed(environmentVariables))
        {
            await WriteAsync(output, ClaudeHookOutcome.Neutral, cancellationToken);
            return ExitCode;
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        var outcome = ClaudeHookOutcome.Neutral;

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
            // ClaudeHookOutcome.Neutral without awaiting `runTask` - a
            // handler ignoring cancellation must not be allowed to keep
            // this call, and the harness, waiting past the timeout.
        }
        catch
        {
            // Fail-open on EVERYTHING: an empty or malformed payload or a
            // handler exception (database contention, a schema version
            // mismatch). `outcome` is still ClaudeHookOutcome.Neutral, so the
            // harness always gets a valid neutral response, never an error.
            outcome = ClaudeHookOutcome.Neutral;
        }

        await WriteAsync(output, outcome, cancellationToken);

        return ExitCode;
    }

    private static async Task<ClaudeHookOutcome> RunHandlerAsync(
        TextReader input,
        Func<ClaudeHookPayload, CancellationToken, Task<ClaudeHookOutcome>> handle,
        CancellationToken cancellationToken)
    {
        var json = await input.ReadToEndAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize(json, ClaudeHookJsonContext.Default.ClaudeHookPayload);

        return payload is null ? ClaudeHookOutcome.Neutral : await handle(payload, cancellationToken);
    }

    // Always success: a hook adapter reports failure to the harness through
    // its own JSON protocol (or silently, via the neutral response), never
    // through the process exit code.
    private const int ExitCode = 0;

    private static bool IsSuppressed(IEnvironmentVariableProvider environmentVariables)
        => environmentVariables.GetEnvironmentVariable("NITRO_HOOK_SUPPRESS") is "1" or "true";

    private static async Task WriteAsync(
        TextWriter output, ClaudeHookOutcome outcome, CancellationToken cancellationToken)
    {
        var response = ToResponse(outcome);
        var json = JsonSerializer.Serialize(response, ClaudeHookJsonContext.Default.ClaudeHookResponse);

        await output.WriteAsync(json.AsMemory(), cancellationToken);
        await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
    }

    private static ClaudeHookResponse ToResponse(ClaudeHookOutcome outcome)
    {
        if (outcome.Block)
        {
            return new ClaudeHookResponse { Decision = "block", Reason = outcome.BlockReason };
        }

        if (outcome.AdditionalContext is { Length: > 0 } context)
        {
            return new ClaudeHookResponse
            {
                HookSpecificOutput = new ClaudeHookSpecificOutput { AdditionalContext = context }
            };
        }

        return new ClaudeHookResponse();
    }
}
