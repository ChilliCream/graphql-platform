using System.Text.Json;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fail-open envelope every <c>nitro agent hook opencode &lt;event&gt;</c>
/// subcommand runs its handler through. A malformed payload, database
/// contention, a schema version mismatch, a missing workspace, any exception a
/// handler raises, and the timeout itself all resolve to the same neutral
/// <c>{}</c> response.
/// </summary>
internal static class OpencodeHookExecutor
{
    /// <summary>
    /// The point past which a hung handler must not be allowed to wedge opencode's
    /// turn any longer.
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
    /// <see cref="EntryTimeout"/>.
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

            // Else: the entry timeout won the race, and outcome stays neutral without
            // awaiting runTask.
        }
        catch (AgentWorkspaceSchemaMismatchException exception)
        {
            // Reported rather than swallowed: a stale schema keeps every hook of
            // every session inert until someone migrates it.
            await error.WriteLineAsync(exception.Message.AsMemory(), cancellationToken);
            await WriteAsync(output, OpencodeHookOutcome.Neutral, cancellationToken);

            return FailureExitCode;
        }
        catch
        {
            // Fail-open on everything else: an empty or malformed payload, or a
            // handler exception.
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

    // Always success: a hook adapter reports failure through its own JSON protocol,
    // never through the process exit code.
    private const int ExitCode = 0;

    /// <summary>
    /// The nonzero exit a hook uses to report a condition the user has to act on.
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
