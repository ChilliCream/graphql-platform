using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Wires an opencode hook leaf command's action through
/// <see cref="OpencodeHookExecutor"/>, the opencode analog of
/// <see cref="ClaudeHookCommandExtensions.SetHookAction"/> and
/// <see cref="CodexHookCommandExtensions.SetCodexHookAction"/>. Unlike Claude and
/// Codex, opencode's response envelope carries no hook-event-name equivalent, so this
/// method takes none.
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
