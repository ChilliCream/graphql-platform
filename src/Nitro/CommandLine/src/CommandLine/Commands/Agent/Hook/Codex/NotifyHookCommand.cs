using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Handles an argv-based Codex notify payload and attempts to run any wrapped
/// foreign notify program afterward. Returns the foreign exit code when available,
/// or zero otherwise.
/// </summary>
internal sealed class NotifyHookCommand : Command
{
    public NotifyHookCommand() : base("notify")
    {
        Description = "Adapt Codex CLI's notify program: queue the unread-mail digest into the thread's "
            + "next turn, then exec any wrapped foreign notify program.";

        Arguments.Add(Opt<NotifyPayloadArgument>.Instance);
        SetAction(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var services = CommandExecutionContext.Services;
        var handler = services.GetRequiredService<ICodexHookHandler>();
        var sidecarStore = services.GetRequiredService<ICodexHooksSidecarStore>();
        var pathResolver = services.GetRequiredService<ICodexPathResolver>();
        var foreignRunner = services.GetRequiredService<ICodexForeignNotifyRunner>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var payloadJson = parseResult.GetRequiredValue(Opt<NotifyPayloadArgument>.Instance);
        return await CodexNotifyExecutor.RunAsync(
            environmentVariables,
            handler.HandleNotifyAsync,
            ct => ExecForeignAsync(sidecarStore, pathResolver, foreignRunner, payloadJson, ct),
            payloadJson,
            cancellationToken);
    }

    private static async Task<int?> ExecForeignAsync(
        ICodexHooksSidecarStore sidecarStore,
        ICodexPathResolver pathResolver,
        ICodexForeignNotifyRunner foreignRunner,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var sidecar = await sidecarStore.ReadAsync(cancellationToken);
        var configTomlPath = pathResolver.ResolveConfigToml();
        var foreignArgv = sidecar.NotifyEntryFor(configTomlPath)?.PriorForeign;

        return foreignArgv is null or { Count: 0 }
            ? null
            : await foreignRunner.RunAsync(foreignArgv, payloadJson, cancellationToken);
    }
}
