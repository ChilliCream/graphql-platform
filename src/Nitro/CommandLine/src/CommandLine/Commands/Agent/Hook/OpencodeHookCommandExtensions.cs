using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;

/// <summary>
/// Binds an Opencode hook command to stdin payload handling through
/// <see cref="OpencodeHookExecutor"/>.
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
