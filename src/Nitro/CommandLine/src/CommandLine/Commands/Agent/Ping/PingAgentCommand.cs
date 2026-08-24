using ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping;

/// <summary>
/// The foreground, synchronous form of Layer C's best-effort wake ping:
/// resolves every live session the given actor has claimed and fires each
/// one in-process, applying the exact same cooldown, lease, and
/// result-write contract the notifier uses for auto-ping, minus the
/// detached spawn - there is no further process to hand off to when the
/// caller is already the foreground command a person ran directly.
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
        var leaseStore = services.GetRequiredService<IPingLeaseStore>();
        var executor = services.GetRequiredService<IPingSessionExecutor>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailAgentName.Normalize(parseResult.GetRequiredValue(Opt<PingActorArgument>.Instance));

        var sessions = await sessionRegistry.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);

        var outcomes = new List<PingSessionResult>(sessions.Count);

        foreach (var session in sessions)
        {
            var outcome = await PingSessionAsync(
                actor, session, sessionRegistry, leaseStore, executor, timeProvider, cancellationToken);
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
    /// Applies the cooldown/lease decision, then either records the
    /// outcome directly (no endpoint, unsupported endpoint kind, cooldown
    /// still active, capacity dropped) or executes the endpoint
    /// transport call in-process via <see cref="IPingSessionExecutor"/>.
    /// Returns a short label for CLI display; every branch that reaches
    /// <see cref="IAgentSessionRegistry.WritePingResultAsync"/> already
    /// wrote the exact same value to the row.
    /// </summary>
    private static async Task<string> PingSessionAsync(
        string actor,
        AgentSessionRecord session,
        IAgentSessionRegistry sessionRegistry,
        IPingLeaseStore leaseStore,
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
            // cooldown as supported transports.
            var unsupportedCooldownClaimed = await sessionRegistry.TryClaimPingCooldownAsync(
                session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

            if (!unsupportedCooldownClaimed)
            {
                return "skipped-cooldown";
            }

            await sessionRegistry.WritePingResultAsync(
                session.Harness, session.SessionId, attemptId, AgentPingResult.Unsupported, null, cancellationToken);
            return AgentPingResult.Unsupported;
        }

        var cooldownClaimed = await sessionRegistry.TryClaimPingCooldownAsync(
            session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

        if (!cooldownClaimed)
        {
            return "skipped-cooldown";
        }

        var slot = await leaseStore.TryAcquireAsync(attemptId, now, PingPolicy.LeaseDuration, cancellationToken);

        if (slot is null)
        {
            await sessionRegistry.WritePingResultAsync(
                session.Harness, session.SessionId, attemptId,
                AgentPingResult.CapacityDropped, null, cancellationToken);
            return AgentPingResult.CapacityDropped;
        }

        if (session.EndpointKind == AgentSessionEndpointKind.ClaudePeer)
        {
            return await executor.ExecuteClaudePeerAsync(
                session.Harness, session.SessionId, actor, session.Pid, attemptId, slot.Value,
                now + PingPolicy.HardTimeout, cancellationToken);
        }

        return await executor.ExecuteCodexThreadAsync(
            session.Harness, session.SessionId, actor, session.EndpointAddr, attemptId, slot.Value,
            now + PingPolicy.HardTimeout, cancellationToken);
    }

    public sealed record PingSessionResult(string Harness, string SessionId, string EndpointKind, string Outcome);
}
