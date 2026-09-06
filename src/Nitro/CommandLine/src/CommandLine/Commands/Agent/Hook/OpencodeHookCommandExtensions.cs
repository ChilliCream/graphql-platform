using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires an opencode hook leaf command's action through
/// <see cref="OpencodeHookExecutor"/>, the opencode analog of
/// <see cref="ClaudeHookCommandExtensions.SetHookAction"/> and
/// <see cref="CodexHookCommandExtensions.SetCodexHookAction"/>: a hook
/// adapter reports failure to opencode through its own JSON protocol,
/// never through stderr or a nonzero exit code. Unlike Claude and Codex,
/// opencode's response envelope carries no hook-event-name equivalent, so
/// this method takes none.
/// <para>
/// Named distinctly from <see cref="ClaudeHookCommandExtensions.SetHookAction"/>
/// and <see cref="CodexHookCommandExtensions.SetCodexHookAction"/> (rather
/// than an overload of either) for the same reason
/// <c>SetCodexHookAction</c> is: two extension methods differing only in a
/// generic delegate parameter's type are ambiguous to the compiler at every
/// call site that can see both, since a lambda argument is applicable to
/// either before type inference disambiguates.
/// </para>
/// </summary>
internal static class OpencodeHookCommandExtensions
{
    public static Command SetOpencodeHookAction(
        this Command command,
        Func<IOpencodeHookHandler, OpencodeHookPayload, CancellationToken, Task<OpencodeHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.Services;
            var handler = services.GetRequiredService<IOpencodeHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            return await OpencodeHookExecutor.RunAsync(
                environmentVariables,
                services.GetRequiredService<IStandardInputReader>().Reader,
                parseResult.InvocationConfiguration.Output,
                parseResult.InvocationConfiguration.Error,
                (payload, ct) => handle(handler, payload, ct),
                cancellationToken);
        });

        return command;
    }
}
