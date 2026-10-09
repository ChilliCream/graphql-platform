using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Binds a Claude hook command to stdin payload handling through
/// <see cref="ClaudeHookExecutor"/>.
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
