using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class Notifier(
    IActorWakeDispatcher dispatcher,
    IAgentSessionRegistry sessionRegistry,
    TimeProvider timeProvider) : INotifier
{
    public async Task NotifyAsync(IReadOnlyList<string> recipientActors, CancellationToken cancellationToken)
    {
        try
        {
            // Fixed once for this whole call and shared across every
            // recipient (and every target within each recipient's batch):
            // WakeDispatchPolicy.BatchDeadline is the budget for the entire
            // notification, not a per-recipient allowance, so a broadcast to
            // many recipients is never bounded by a multiple of it.
            var deadline = timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;

            foreach (var actor in recipientActors.Distinct(StringComparer.Ordinal))
            {
                try
                {
                    if (await HasLiveDbWatchSessionAsync(actor, cancellationToken))
                    {
                        // Commit-delivered: a live Nitro board session for
                        // this actor already observes the just-committed
                        // mail row through its own db-file watcher, the
                        // moment it lands, with nothing further to attempt.
                        // Falling through to the dispatcher would resolve
                        // this same live session as a target with no
                        // routable transport and record a false unsupported/
                        // error outcome instead of the true delivery this
                        // endpoint already represents.
                        continue;
                    }

                    await dispatcher.DispatchAsync(actor, deadline, cancellationToken);
                }
                catch
                {
                    // Fail-open per recipient: a dispatch failure for one
                    // recipient must not stop the notifier from dispatching
                    // the rest, and can never surface to the caller.
                }
            }
        }
        catch
        {
            // Belt and suspenders on top of every per-recipient try/catch
            // above: mail success output and exit code can NEVER be altered
            // by a notification failure (the notifier contract).
        }
    }

    /// <summary>
    /// True when <paramref name="actor"/> has at least one live, current-
    /// instance session whose endpoint is <see cref="AgentSessionEndpointKind.DbWatch"/>
    /// (an open Nitro board): its mail is already delivered by the commit
    /// itself, so no transport attempt is needed for it. An actor with no
    /// such session (including one with only coding-harness sessions)
    /// falls through to the ordinary dispatcher.
    /// </summary>
    private async Task<bool> HasLiveDbWatchSessionAsync(string actor, CancellationToken cancellationToken)
    {
        var sessions = await sessionRegistry.FindLiveClaimedByAgentNameAsync(actor, cancellationToken);

        return sessions.Any(session => session.EndpointKind == AgentSessionEndpointKind.DbWatch);
    }
}
