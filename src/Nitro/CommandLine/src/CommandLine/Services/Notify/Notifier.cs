namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal sealed class Notifier(IActorWakeDispatcher dispatcher) : INotifier
{
    public async Task NotifyAsync(IReadOnlyList<string> recipientActors, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var actor in recipientActors.Distinct(StringComparer.Ordinal))
            {
                try
                {
                    await dispatcher.DispatchAsync(actor, cancellationToken);
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
