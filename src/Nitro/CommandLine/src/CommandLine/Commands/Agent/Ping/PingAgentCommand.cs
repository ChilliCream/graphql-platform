using ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping;

/// <summary>
/// The foreground, synchronous form of Layer C's best-effort wake ping:
/// resolves every live session the given actor has claimed and fires each
/// one in-process, reserving the exact same <see cref="ISessionGateCoordinator"/>
/// gate and lease slot the notifier's <c>ActorWakeDispatcher</c> reserves
/// for auto-ping, so a manual ping and a concurrent auto-ping (or a future
/// daemon) can never both hold a transport attempt against the same session
/// generation. There is no detached spawn to hand off to - the caller is
/// already the foreground command a person ran directly.
/// </summary>
internal sealed class PingAgentCommand : Command
{
    public PingAgentCommand() : base("ping")
    {
        Description = "Fire the best-effort wake ping at every live session an agent has claimed.";

        Arguments.Add(Opt<PingActorArgument>.Instance);

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent ping codex-worker-1");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
        var gateCoordinator = services.GetRequiredService<ISessionGateCoordinator>();
        var executor = services.GetRequiredService<IPingSessionExecutor>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailAgentName.Normalize(parseResult.GetRequiredValue(Opt<PingActorArgument>.Instance));

        var sessions = await sessionRegistry.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);

        var outcomes = new List<PingSessionResult>(sessions.Count);

        foreach (var session in sessions)
        {
            var outcome = await PingSessionAsync(
                actor, session, sessionRegistry, gateCoordinator, executor, timeProvider, cancellationToken);
            outcomes.Add(new PingSessionResult(session.Harness, session.SessionId, session.EndpointKind, outcome));
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<PingSessionResult>(outcomes));
            return ExitCodes.Success;
        }

        if (outcomes.Count == 0)
        {
            console.WriteLine($"No live sessions for '{actor.EscapeMarkup()}'.");
            return ExitCodes.Success;
        }

        foreach (var outcome in outcomes)
        {
            console.WriteLine(
                $"{outcome.Harness.EscapeMarkup()}  {outcome.SessionId.EscapeMarkup()}  "
                + $"{outcome.EndpointKind.EscapeMarkup()}  {outcome.Outcome.EscapeMarkup()}");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// For a supported endpoint (codex-thread or claude-peer), reserves the
    /// session's <see cref="ISessionGateCoordinator"/> gate and lease slot
    /// exactly the way the notifier's <c>ActorWakeDispatcher</c> does, then
    /// either records the outcome directly (capacity dropped) or executes
    /// the endpoint transport call in-process via <see cref="IPingSessionExecutor"/>.
    /// A busy gate (another attempt already in flight, or a prior success's
    /// cooldown) is skipped without touching the row at all. Success is the
    /// only outcome that starts a cooldown; every other completion releases
    /// the gate exactly once in <c>finally</c>. An unsupported endpoint kind
    /// never reserves the gate or a lease slot: it coalesces under its own
    /// per-session cooldown on <c>last_ping_attempt</c> instead, so a
    /// permanently unsupported endpoint never competes for the shared
    /// ping_leases capacity. Returns a short label for CLI display; every
    /// branch that reaches <see cref="IAgentSessionRegistry.WritePingResultAsync"/>
    /// already wrote the exact same value to the row.
    /// </summary>
    private static async Task<string> PingSessionAsync(
        string actor,
        AgentSessionRecord session,
        IAgentSessionRegistry sessionRegistry,
        ISessionGateCoordinator gateCoordinator,
        IPingSessionExecutor executor,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (session.EndpointKind == AgentSessionEndpointKind.None)
        {
            return "skipped-no-endpoint";
        }

        var now = timeProvider.GetUtcNow();
        var attemptId = MemoryId.New(now);

        if (session.EndpointKind is not AgentSessionEndpointKind.CodexThread
            and not AgentSessionEndpointKind.ClaudePeer)
        {
            // Unsupported attempts coalesce under the same per-session
            // cooldown as supported transports, so repeated manual pings
            // against a permanently unsupported endpoint do not churn the
            // gate. This never reaches the gate coordinator at all: an
            // unsupported endpoint can never transport, so it must never
            // reserve a ping_leases slot a supported session could use.
            var claimed = await sessionRegistry.TryClaimPingCooldownAsync(
                session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

            if (!claimed)
            {
                return "skipped-cooldown";
            }

            await sessionRegistry.WritePingResultAsync(
                session.Harness, session.SessionId, attemptId, AgentPingResult.Unsupported, null, cancellationToken);
            return AgentPingResult.Unsupported;
        }

        var generation = new AgentSessionGeneration(
            session.Harness, session.SessionId, session.Host, session.Pid, session.ProcStart);

        var reservation = await gateCoordinator.TryReserveAsync(generation, attemptId, now, cancellationToken);

        if (reservation.Reservation is null)
        {
            if (reservation.Failure == WakeReservationFailure.GateBusy)
            {
                return "skipped-cooldown";
            }

            // CapacityDropped: the gate coordinator already released the
            // gate it briefly held for nothing, so nothing is left to
            // complete here - only the row still needs an explicit record.
            return await RecordDirectResultAsync(
                sessionRegistry, session, attemptId, now, AgentPingResult.CapacityDropped, cancellationToken);
        }

        var held = reservation.Reservation;
        var success = false;

        try
        {
            // Stamps this attempt onto the row's last_ping_attempt with no
            // cooldown of its own (the gate above already owns cooldown):
            // this call's only remaining purpose is fencing the result
            // write below against a stale completion. A false return means
            // the exact session generation no longer matches a row (ended
            // or rebound since this attempt reserved the gate), so neither
            // a result write nor a transport call follows.
            var stamped = await sessionRegistry.TryClaimPingCooldownAsync(
                session, attemptId, now, TimeSpan.Zero, cancellationToken);

            if (!stamped)
            {
                return "skipped-session-gone";
            }

            var deadline = now + PingPolicy.HardTimeout;

            var outcome = session.EndpointKind == AgentSessionEndpointKind.ClaudePeer
                ? await executor.ExecuteClaudePeerAsync(
                    session.Harness, session.SessionId, actor, session.Pid, attemptId, held.Slot, deadline,
                    cancellationToken)
                : await executor.ExecuteCodexThreadAsync(
                    session.Harness, session.SessionId, actor, session.EndpointAddr, attemptId, held.Slot, deadline,
                    cancellationToken);

            success = outcome.Reason == PingAttemptReason.Ok;
            return outcome.Result;
        }
        finally
        {
            await gateCoordinator.CompleteAsync(held, success, timeProvider.GetUtcNow(), CancellationToken.None);
        }
    }

    /// <summary>
    /// Stamps <paramref name="attemptId"/> onto the row (zero cooldown, so
    /// the claim always succeeds unless the exact session generation no
    /// longer matches a row) and writes <paramref name="result"/>, for a
    /// completion that was decided without ever attempting a transport
    /// call.
    /// </summary>
    private static async Task<string> RecordDirectResultAsync(
        IAgentSessionRegistry sessionRegistry,
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        string result,
        CancellationToken cancellationToken)
    {
        var stamped = await sessionRegistry.TryClaimPingCooldownAsync(
            session, attemptId, now, TimeSpan.Zero, cancellationToken);

        if (!stamped)
        {
            return "skipped-session-gone";
        }

        await sessionRegistry.WritePingResultAsync(
            session.Harness, session.SessionId, attemptId, result, null, cancellationToken);
        return result;
    }

    public sealed record PingSessionResult(string Harness, string SessionId, string EndpointKind, string Outcome);
}
