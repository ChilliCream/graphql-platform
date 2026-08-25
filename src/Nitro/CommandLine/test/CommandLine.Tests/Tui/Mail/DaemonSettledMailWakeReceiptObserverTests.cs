using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// Covers <see cref="DaemonSettledMailWakeReceiptObserver"/>: it re-observes
/// on the daemon's admission poll interval while an observation is still
/// pending, and gives up once the shared batch deadline elapses.
/// </summary>
public sealed class DaemonSettledMailWakeReceiptObserverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailWakeReceipt Receipt(string actor = "bob", long generation = 1) =>
        new() { Actor = actor, Generation = generation };

    [Fact]
    public async Task ObserveAsync_Should_ReturnTheSettledObservation_When_ARetryObservesItResolved()
    {
        // arrange: the inner observer reports Pending on the first read,
        // then Delegated once the daemon's admission loop has claimed it.
        var inner = new FakeMailWakeReceiptObserver();
        inner.SequenceByActor["bob"] = new Queue<MailWakeObservation>(
        [
            FakeMailWakeReceiptObserver.Observation("bob", MailWakeTargetStatus.Pending),
            FakeMailWakeReceiptObserver.Observation("bob", MailWakeTargetStatus.Delegated)
        ]);
        var timeProvider = new FakeTimeProvider(Now);
        var observer = new DaemonSettledMailWakeReceiptObserver(inner, timeProvider);
        var deadline = Now + WakeDispatchPolicy.BatchDeadline;
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var observeTask = observer.ObserveAsync(Receipt(), deadline, cancellationToken);

        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (inner.ObserveCallCount < 1)
        {
            await Task.Delay(5, pollCts.Token);
        }

        timeProvider.Advance(MailWakeDaemonPolicy.Default.AdmissionPollInterval);
        var observation = await observeTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delegated, observation.Status);
        Assert.Equal(2, inner.ObserveCallCount);
    }

    [Fact]
    public async Task ObserveAsync_Should_ReturnTheLastPendingObservation_When_TheBatchDeadlineElapses()
    {
        // arrange: the inner observer never settles, so the decorator keeps
        // re-observing until WakeDispatchPolicy.BatchDeadline elapses, then
        // gives up and returns the still-pending observation.
        var inner = new FakeMailWakeReceiptObserver();
        var timeProvider = new FakeTimeProvider(Now);
        var observer = new DaemonSettledMailWakeReceiptObserver(inner, timeProvider);
        var deadline = Now + WakeDispatchPolicy.BatchDeadline;
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var observeTask = observer.ObserveAsync(Receipt(), deadline, cancellationToken);

        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (inner.ObserveCallCount < 1)
        {
            await Task.Delay(5, pollCts.Token);
        }

        timeProvider.Advance(WakeDispatchPolicy.BatchDeadline + MailWakeDaemonPolicy.Default.AdmissionPollInterval);
        var observation = await observeTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Pending, observation.Status);
        Assert.True(inner.ObserveCallCount > 1);
    }

    [Fact]
    public async Task ObserveAsync_Should_HonorTheCallersDeadline_When_ItHasAlreadyElapsed()
    {
        // arrange: the caller's own deadline (shared across every receipt of
        // one MailWakeDispatch.RunAsync batch) has already elapsed by the
        // time this receipt is observed, as it would for a later recipient
        // once an earlier one's retry loop spent the whole shared window.
        // The decorator must honor that deadline rather than computing its
        // own fresh WakeDispatchPolicy.BatchDeadline from the current clock
        // and retrying regardless of what the caller passed in.
        var inner = new FakeMailWakeReceiptObserver();
        var timeProvider = new FakeTimeProvider(Now);
        var observer = new DaemonSettledMailWakeReceiptObserver(inner, timeProvider);
        var deadline = Now - TimeSpan.FromSeconds(1);
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var observation = await observer.ObserveAsync(Receipt(), deadline, cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert: gave up after the single initial read, never entering the
        // retry loop, instead of polling for its own fresh BatchDeadline.
        Assert.Equal(MailWakeTargetStatus.Pending, observation.Status);
        Assert.Equal(1, inner.ObserveCallCount);
    }
}
