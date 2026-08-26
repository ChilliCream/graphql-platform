using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// An <see cref="IMailWakeReceiptObserver"/> decorator used by the unified
/// dashboard's mail tab: since <see cref="DaemonOwnedActorWakeDispatcher"/>
/// never dispatches directly, a single observation right after enqueue can
/// still see <see cref="MailWakeTargetStatus.Pending"/> only because the
/// already-running daemon's own admission loop has not claimed the
/// generation yet, not because delivery actually failed. This re-observes
/// on <see cref="MailWakeDaemonPolicy.Default"/>'s admission poll interval
/// until the generation settles or the caller-supplied deadline elapses, so
/// the returned <see cref="MailWakeObservation"/> is truthful about whether
/// the daemon settled it in time. The deadline always comes from the caller
/// (see <see cref="IMailWakeReceiptObserver.ObserveAsync"/>) rather than
/// being computed here, so every receipt of one
/// <see cref="MailWakeDispatch.RunAsync"/> call shares the same
/// <see cref="WakeDispatchPolicy.BatchDeadline"/> instead of each recipient
/// getting its own fresh one.
/// </summary>
internal sealed class DaemonSettledMailWakeReceiptObserver(
    IMailWakeReceiptObserver inner, TimeProvider timeProvider) : IMailWakeReceiptObserver
{
    public async Task<MailWakeObservation> ObserveAsync(
        MailWakeReceipt receipt, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var observation = await inner.ObserveAsync(receipt, deadline, cancellationToken).ConfigureAwait(false);

        while (observation.Status == MailWakeTargetStatus.Pending && timeProvider.GetUtcNow() < deadline)
        {
            await Task.Delay(MailWakeDaemonPolicy.Default.AdmissionPollInterval, timeProvider, cancellationToken)
                .ConfigureAwait(false);
            observation = await inner.ObserveAsync(receipt, deadline, cancellationToken).ConfigureAwait(false);
        }

        return observation;
    }
}
