using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires a Claude hook leaf command's action through
/// <see cref="ClaudeHookExecutor"/> instead of
/// <c>SetActionWithExceptionHandling</c>: a hook adapter reports a transient
/// failure to the harness through its own JSON protocol rather than through
/// stderr, and only a condition the user has to act on (an unmigrated
/// workspace) reaches stderr with a nonzero exit.
/// </summary>
internal static class ClaudeHookCommandExtensions
{
    public static Command SetHookAction(
        this Command command,
        string hookEventName,
        Func<IClaudeHookHandler, ClaudeHookPayload, CancellationToken, Task<ClaudeHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.Services;
            var handler = services.GetRequiredService<IClaudeHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            return await ClaudeHookExecutor.RunAsync(
                environmentVariables,
                services.GetRequiredService<IStandardInputReader>().Reader,
                parseResult.InvocationConfiguration.Output,
                parseResult.InvocationConfiguration.Error,
                (payload, ct) => handle(handler, payload, ct),
                hookEventName,
                cancellationToken);
        });

        return command;
    }
}
