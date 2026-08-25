using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An in-memory <see cref="IMailWakeReceiptObserver"/> a test configures per
/// actor through <see cref="StatusByActor"/>; an actor with no entry
/// observes <see cref="MailWakeTargetStatus.Pending"/>, the real observer's
/// own truthful default for a generation no batch has claimed yet. Set
/// <see cref="Gate"/> to make every <see cref="ObserveAsync"/> call await it
/// first, for exercising cancellation while an observation is in flight.
/// </summary>
internal sealed class FakeMailWakeReceiptObserver : IMailWakeReceiptObserver
{
    public Dictionary<string, MailWakeObservation> StatusByActor { get; } = new(StringComparer.Ordinal);

    public int ObserveCallCount { get; private set; }

    public TaskCompletionSource? Gate { get; set; }

    public async Task<MailWakeObservation> ObserveAsync(MailWakeReceipt receipt, CancellationToken cancellationToken)
    {
        ObserveCallCount++;

        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (StatusByActor.TryGetValue(receipt.Actor, out var observation))
        {
            return observation;
        }

        return new MailWakeObservation(
            receipt.Actor, receipt.Generation, MailWakeTargetStatus.Pending, IsZero: false, Targets: []);
    }

    /// <summary>
    /// Builds a zero-target observation for <paramref name="status"/>,
    /// ready to assign into <see cref="StatusByActor"/>.
    /// </summary>
    public static MailWakeObservation Observation(string actor, string status, long generation = 1) =>
        new(actor, generation, status, WakeReceiptAggregator.IsZero(status), []);
}
