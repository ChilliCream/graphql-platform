using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires a Claude hook leaf command's action through
/// <see cref="ClaudeHookExecutor"/> instead of
/// <c>SetActionWithExceptionHandling</c>: a hook adapter reports failure to
/// the harness through its own JSON protocol, never through stderr or a
/// nonzero exit code.
/// </summary>
internal static class ClaudeHookCommandExtensions
{
    public static Command SetHookAction(
        this Command command,
        string hookEventName,
        Func<IClaudeHookHandler, ClaudeHookPayload, bool, CancellationToken, Task<ClaudeHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.s_services.Value!;
            var handler = services.GetRequiredService<IClaudeHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            var dryRun = parseResult.GetValue(Opt<DryRunHookOption>.Instance);

            return await ClaudeHookExecutor.RunAsync(
                environmentVariables,
                Console.In,
                parseResult.InvocationConfiguration.Output,
                (payload, ct) => handle(handler, payload, dryRun, ct),
                hookEventName,
                cancellationToken);
        });

        return command;
    }
}
