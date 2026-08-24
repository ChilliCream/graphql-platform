using ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping;

/// <summary>
/// The detached worker <c>Notifier</c> spawns for a supported endpoint
/// ping attempt once its cooldown claim and lease slot are already secured:
/// not meant for direct interactive use, it carries every identifier it
/// needs as flags because it runs as its own, independently-launched
/// process, never in-process with the notifier that spawned it.
/// </summary>
internal sealed class PingWorkerCommand : Command
{
    public PingWorkerCommand() : base("ping-worker")
    {
        Description = "Internal: performs one already-leased ping attempt. "
            + "Spawned by the notifier; not for direct use.";

        Options.Add(Opt<PingWorkerHarnessOption>.Instance);
        Options.Add(Opt<PingWorkerSessionIdOption>.Instance);
        Options.Add(Opt<PingWorkerActorOption>.Instance);
        Options.Add(Opt<PingWorkerEndpointKindOption>.Instance);
        Options.Add(Opt<PingWorkerEndpointAddrOption>.Instance);
        Options.Add(Opt<PingWorkerPidOption>.Instance);
        Options.Add(Opt<PingWorkerAttemptOption>.Instance);
        Options.Add(Opt<PingWorkerSlotOption>.Instance);
        Options.Add(Opt<PingWorkerDeadlineOption>.Instance);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var executor = services.GetRequiredService<IPingSessionExecutor>();

        var harness = parseResult.GetRequiredValue(Opt<PingWorkerHarnessOption>.Instance);
        var sessionId = parseResult.GetRequiredValue(Opt<PingWorkerSessionIdOption>.Instance);
        var actor = parseResult.GetRequiredValue(Opt<PingWorkerActorOption>.Instance);
        var endpointKind = parseResult.GetRequiredValue(Opt<PingWorkerEndpointKindOption>.Instance);
        var endpointAddr = parseResult.GetRequiredValue(Opt<PingWorkerEndpointAddrOption>.Instance);
        var pid = parseResult.GetRequiredValue(Opt<PingWorkerPidOption>.Instance);
        var attempt = parseResult.GetRequiredValue(Opt<PingWorkerAttemptOption>.Instance);
        var slot = parseResult.GetRequiredValue(Opt<PingWorkerSlotOption>.Instance);
        var deadline = parseResult.GetRequiredValue(Opt<PingWorkerDeadlineOption>.Instance);

        if (endpointKind == AgentSessionEndpointKind.ClaudePeer)
        {
            await executor.ExecuteClaudePeerAsync(
                harness, sessionId, actor, pid, attempt, slot, deadline, cancellationToken);
        }
        else
        {
            await executor.ExecuteCodexThreadAsync(
                harness, sessionId, actor, endpointAddr, attempt, slot, deadline, cancellationToken);
        }

        // Always success: nobody reads this detached child's exit code, and
        // its own outcome is already durably recorded in agent_sessions.
        return ExitCodes.Success;
    }
}
