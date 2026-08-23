using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires a Copilot stdin-based hook leaf command's action through
/// <see cref="CopilotHookExecutor"/>, the Copilot analog of
/// <see cref="ClaudeHookCommandExtensions.SetHookAction"/>/<see cref="CodexHookCommandExtensions.SetCodexHookAction"/>:
/// a hook adapter reports failure to Copilot through its own JSON protocol,
/// never through stderr or a nonzero exit code.
/// <para>
/// Named distinctly from the other two harnesses' equivalents for the same
/// reason <see cref="CodexHookCommandExtensions.SetCodexHookAction"/>
/// documents: two extension methods differing only in a generic delegate
/// parameter's type are ambiguous to the compiler at every call site that
/// can see both.
/// </para>
/// </summary>
internal static class CopilotHookCommandExtensions
{
    public static Command SetCopilotHookAction(
        this Command command,
        Func<ICopilotHookHandler, CopilotHookPayload, bool, CancellationToken, Task<CopilotHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.s_services.Value!;
            var handler = services.GetRequiredService<ICopilotHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            var dryRun = parseResult.GetValue(Opt<DryRunHookOption>.Instance);

            return await CopilotHookExecutor.RunAsync(
                environmentVariables,
                Console.In,
                parseResult.InvocationConfiguration.Output,
                (payload, ct) => handle(handler, payload, dryRun, ct),
                cancellationToken);
        });

        return command;
    }
}
