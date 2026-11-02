using System.Text.Json;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Executes Codex hook handlers with a timeout and writes their JSON responses.
/// Malformed input, handler failures, and timeouts produce a neutral response; a
/// schema mismatch also writes a diagnostic to stderr and returns exit code 1.
/// </summary>
internal static class CodexHookExecutor
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
        Func<CodexHookPayload, CancellationToken, Task<CodexHookOutcome>> handle,
        string hookEventName,
        CancellationToken cancellationToken)
        => RunAsync(
            environmentVariables, input, output, error, handle, hookEventName, EntryTimeout, cancellationToken);

    internal static async Task<int> RunAsync(
        IEnvironmentVariableProvider environmentVariables,
        TextReader input,
        TextWriter output,
        TextWriter error,
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

            // A timeout leaves the outcome neutral without waiting for the handler to finish.
        }
        catch (AgentWorkspaceSchemaMismatchException exception)
        {
            await error.WriteLineAsync(exception.Message.AsMemory(), cancellationToken);
            await WriteAsync(output, CodexHookOutcome.Neutral, hookEventName, cancellationToken);

            return FailureExitCode;
        }
        catch
        {
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

    private const int ExitCode = 0;

    /// <summary>
    /// The exit code returned for a workspace schema mismatch.
    /// </summary>
    private const int FailureExitCode = 1;

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
