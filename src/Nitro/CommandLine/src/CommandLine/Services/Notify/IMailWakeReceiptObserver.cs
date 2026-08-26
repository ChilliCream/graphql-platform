using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Reads back a <see cref="MailWakeReceipt"/>'s durable dispatch state:
/// which batch (if any) has claimed its generation, and what every target
/// that batch reached has recorded so far. Read-only, no caching: each call
/// runs a fresh read transaction, so two calls observe whatever changed
/// between them.
/// </summary>
internal interface IMailWakeReceiptObserver
{
    /// <summary>
    /// Reads back <paramref name="receipt"/>'s durable dispatch state.
    /// </summary>
    /// <param name="receipt">The wake generation to observe.</param>
    /// <param name="deadline">
    /// The caller's own shared batch deadline (for example
    /// <see cref="WakeDispatchPolicy.BatchDeadline"/> computed once for
    /// every receipt of one send). A single-read implementation may ignore
    /// it, while an implementation that retries until settlement must stop
    /// by it rather than computing a fresh deadline of its own per call.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<MailWakeObservation> ObserveAsync(
        MailWakeReceipt receipt, DateTimeOffset deadline, CancellationToken cancellationToken);
}
