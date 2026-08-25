using ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping;

/// <summary>
/// A detached worker for a single, already-reserved endpoint ping attempt:
/// not meant for direct interactive use, it carries every identifier it
/// needs as flags so it can run as its own, independently-launched process.
/// The foreground notifier (<c>Notifier</c>/<c>ActorWakeDispatcher</c>) no
/// longer spawns this command for automatic mail wake - it dispatches every
/// target in-process instead - so this command currently has no production
/// caller; it is retained only as the explicit, non-mail compatibility path
/// <see cref="Services.Notify.IPingWorkerLauncher"/> still exists for
/// (a caller that has itself already reserved a session gate and lease slot
/// out of process, e.g. a future out-of-process daemon).
/// </summary>
internal sealed class PingWorkerCommand : Command
{
    public PingWorkerCommand() : base("ping-worker")
    {
        Description = "Internal: performs one already-leased ping attempt. "
            + "Not spawned by the notifier; kept only for an explicit, non-mail out-of-process caller.";

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
