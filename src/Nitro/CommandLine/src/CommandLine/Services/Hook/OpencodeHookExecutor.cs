using System.Text.Json;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Executes Opencode hook handlers with a timeout and writes their JSON responses.
/// Malformed input, handler failures, and timeouts produce a neutral response; a
/// schema mismatch also writes a diagnostic to stderr and returns exit code 1.
/// </summary>
internal static class OpencodeHookExecutor
{
    /// <summary>
    /// The default timeout for reading the payload and running the handler.
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

            // A timeout leaves the outcome neutral without waiting for the handler to finish.
        }
        catch (AgentWorkspaceSchemaMismatchException exception)
        {
            await error.WriteLineAsync(exception.Message.AsMemory(), cancellationToken);
            await WriteAsync(output, OpencodeHookOutcome.Neutral, cancellationToken);

            return FailureExitCode;
        }
        catch
        {
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

    private const int ExitCode = 0;

    /// <summary>
    /// The exit code returned for a workspace schema mismatch.
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
