namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class Notifier(IActorWakeDispatcher dispatcher, TimeProvider timeProvider) : INotifier
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
}
