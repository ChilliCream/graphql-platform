using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Options;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hook.Codex;

/// <summary>
/// Adapts Codex CLI's <c>notify</c> mechanism: the idle-turn gate. Queues one
/// ledger-claimed unread-mail digest into the thread via <c>codex queue --thread</c>,
/// then execs any foreign <c>notify</c> program this
/// install wrapped, preserving argv/stdin/cwd and finishing with the
/// foreign program's own exit code (install-flow contract: "ours execs it
/// after our work... if our handler fails, the foreign program still
/// runs"). Not wired through <c>CodexHookCommandExtensions</c>: this reads
/// its payload from argv, not stdin, and its exit code carries meaning,
/// unlike every other command in this tree.
/// </summary>
internal sealed class NotifyHookCommand : Command
{
    public NotifyHookCommand() : base("notify")
    {
        Description = "Adapt Codex CLI's notify program: queue the unread-mail digest into the thread's "
            + "next turn, then exec any wrapped foreign notify program.";

        Arguments.Add(Opt<NotifyPayloadArgument>.Instance);
        Options.Add(Opt<DryRunHookOption>.Instance);

        SetAction(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var services = CommandExecutionContext.s_services.Value!;
        var handler = services.GetRequiredService<ICodexHookHandler>();
        var sidecarStore = services.GetRequiredService<ICodexHooksSidecarStore>();
        var pathResolver = services.GetRequiredService<ICodexPathResolver>();
        var foreignRunner = services.GetRequiredService<ICodexForeignNotifyRunner>();
        var environmentVariables = services.GetRequiredService<IEnvironmentVariableProvider>();

        var payloadJson = parseResult.GetRequiredValue(Opt<NotifyPayloadArgument>.Instance);
        var dryRun = parseResult.GetValue(Opt<DryRunHookOption>.Instance);

        return await CodexNotifyExecutor.RunAsync(
            environmentVariables,
            (payload, ct) => handler.HandleNotifyAsync(payload, dryRun, ct),
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
