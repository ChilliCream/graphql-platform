using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class Notifier(
    IAgentSessionRegistry sessionRegistry,
    IPingLeaseStore leaseStore,
    IPingWorkerLauncher launcher,
    ILaunchDescriptorResolver launchDescriptorResolver,
    TimeProvider timeProvider) : INotifier
{
    public async Task NotifyAsync(IReadOnlyList<string> recipientActors, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var actor in recipientActors.Distinct(StringComparer.Ordinal))
            {
                IReadOnlyList<AgentSessionRecord> sessions;

                try
                {
                    sessions = await sessionRegistry.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);
                }
                catch
                {
                    // Fail-open per recipient: a lookup failure for one
                    // recipient must not stop the notifier from firing at
                    // the rest, and can never surface to the caller.
                    continue;
                }

                foreach (var session in sessions)
                {
                    await NotifySessionAsync(actor, session, cancellationToken);
                }
            }
        }
        catch
        {
            // Belt and suspenders on top of every per-recipient/per-session
            // try/catch below: mail success output and exit code can NEVER
            // be altered by a notification failure (the notifier contract).
        }
    }

    private async Task NotifySessionAsync(
        string actor, AgentSessionRecord session, CancellationToken cancellationToken)
    {
        try
        {
            if (session.EndpointKind == AgentSessionEndpointKind.None)
            {
                return;
            }

            var now = timeProvider.GetUtcNow();
            var attemptId = MemoryId.New(now);

            if (session.EndpointKind != AgentSessionEndpointKind.CodexThread)
            {
                // Every endpoint kind other than codex-thread and none is
                // recorded, not attempted: the notifier has no transport
                // for it (claude-peer today; a future kind would land here
                // too until it gets its own handler). Unsupported attempts
                // coalesce under the same per-session cooldown as codex-thread.
                var unsupportedCooldownClaimed = await sessionRegistry.TryClaimPingCooldownAsync(
                    session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

                if (!unsupportedCooldownClaimed)
                {
                    return;
                }

                await sessionRegistry.WritePingResultAsync(
                    session.Harness, session.SessionId, attemptId,
                    AgentPingResult.Unsupported, null, cancellationToken);
                return;
            }

            var cooldownClaimed = await sessionRegistry.TryClaimPingCooldownAsync(
                session, attemptId, now, PingPolicy.Cooldown, cancellationToken);

            if (!cooldownClaimed)
            {
                // Still within the per-session cooldown: coalesced, not
                // recorded as a distinct attempt.
                return;
            }

            var slot = await leaseStore.TryAcquireAsync(
                attemptId, now, PingPolicy.LeaseDuration, cancellationToken);

            if (slot is null)
            {
                await sessionRegistry.WritePingResultAsync(
                    session.Harness, session.SessionId, attemptId,
                    AgentPingResult.CapacityDropped, null, cancellationToken);
                return;
            }

            var descriptor = launchDescriptorResolver.Resolve();

            var workerArgs = new[]
            {
                "agent", "ping-worker",
                "--harness", session.Harness,
                "--session-id", session.SessionId,
                "--actor", actor,
                "--endpoint-addr", session.EndpointAddr,
                "--attempt", attemptId,
                "--slot", slot.Value.ToString(CultureInfo.InvariantCulture)
            };

            if (!launcher.TryLaunch(descriptor, workerArgs))
            {
                await leaseStore.ReleaseAsync(slot.Value, attemptId, cancellationToken);
                await sessionRegistry.WritePingResultAsync(
                    session.Harness, session.SessionId, attemptId,
                    AgentPingResult.SpawnFailed, null, cancellationToken);
            }
        }
        catch
        {
            // A failed ping is a non-event: never propagate, never affect
            // the mail command that triggered this notification.
        }
    }
}
