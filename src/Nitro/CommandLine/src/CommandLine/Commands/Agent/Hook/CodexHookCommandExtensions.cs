using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires a Codex stdin-based hook leaf command's action through
/// <see cref="CodexHookExecutor"/>, the Codex analog of
/// <see cref="ClaudeHookCommandExtensions.SetHookAction"/>: a hook adapter
/// reports failure to Codex through its own JSON protocol, never through
/// stderr or a nonzero exit code. Covers <c>session-start</c>,
/// <c>user-prompt-submit</c>, and <c>session-end</c> only - <c>notify</c>
/// reads argv, not stdin, and is wired separately in
/// <c>NotifyHookCommand</c>.
/// <para>
/// Named distinctly from <see cref="ClaudeHookCommandExtensions.SetHookAction"/>
/// (rather than an overload of the same name) even though both live under
/// the <c>ChilliCream.Nitro.CommandLine.Commands.Agent.Hook</c> namespace
/// tree: two extension methods differing only in a generic delegate
/// parameter's type are ambiguous to the compiler at every call site that
/// can see both (a lambda argument is applicable to either before type
/// inference disambiguates), which broke Claude's own leaf commands the
/// moment this file was added alongside them.
/// </para>
/// </summary>
internal static class CodexHookCommandExtensions
{
    public static Command SetCodexHookAction(
        this Command command,
        string hookEventName,
        Func<ICodexHookHandler, CodexHookPayload, bool, CancellationToken, Task<CodexHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.s_services.Value!;
            var handler = services.GetRequiredService<ICodexHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            var dryRun = parseResult.GetValue(Opt<DryRunHookOption>.Instance);

            return await CodexHookExecutor.RunAsync(
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
