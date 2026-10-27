using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Binds a Codex hook command to stdin payload handling through
/// <see cref="CodexHookExecutor"/>.
/// </summary>
internal static class CodexHookCommandExtensions
{
    public static Command SetCodexHookAction(
        this Command command,
        string hookEventName,
        Func<ICodexHookHandler, CodexHookPayload, CancellationToken, Task<CodexHookOutcome>> handle)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var services = CommandExecutionContext.Services;
            var handler = services.GetRequiredService<ICodexHookHandler>();
            var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();
            return await CodexHookExecutor.RunAsync(
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
