using Microsoft.Extensions.Time.Testing;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public class MessageBusSchedulerSignalTests
{
    private static readonly DateTimeOffset s_baseTime =
        new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WaitUntilAsync_Should_ReturnImmediately_When_WakeTimeIsInPast()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        await signal.WaitUntilAsync(s_baseTime.AddMinutes(-5), cts.Token);

        // assert
        Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_ReturnImmediately_When_WakeTimeEqualsNow()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        await signal.WaitUntilAsync(s_baseTime, cts.Token);

        // assert
        Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_ReturnWhenTimeReached_When_TimeAdvances()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        var waitTask = signal.WaitUntilAsync(s_baseTime.AddMinutes(3), CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.False(waitTask.IsCompleted);

        // act
        timeProvider.Advance(TimeSpan.FromMinutes(3));

        // assert
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_CapDelayAtFiveMinutes_When_WakeTimeIsFarInFuture()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        var waitTask = signal.WaitUntilAsync(s_baseTime.AddYears(1), CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // act - advance past 5 minutes to prove the delay was capped (not 1 year)
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        // assert
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_RespectCancellation_When_TokenCancelledDuringWait()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        using var cts = new CancellationTokenSource();
        var waitTask = signal.WaitUntilAsync(s_baseTime.AddMinutes(5), cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // act
        await cts.CancelAsync();

        // assert
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_ReturnEarly_When_NotifyCalledWithEarlierTime()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        var waitTask = signal.WaitUntilAsync(s_baseTime.AddMinutes(5), CancellationToken.None);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // act
        signal.Notify(s_baseTime.AddMinutes(3));

        // assert - should return promptly, dispatcher will re-query and re-sleep
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
    }

    [Fact]
    public async Task Notify_Should_NotWake_When_ScheduledTimeIsLaterThanCurrentTarget()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var waitTask = signal.WaitUntilAsync(s_baseTime.AddMinutes(1), cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // act
        signal.Notify(s_baseTime.AddMinutes(2));

        // assert
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task Notify_Should_NotWake_When_ScheduledTimeEqualsCurrentTarget()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        var wakeTime = s_baseTime.AddMinutes(3);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var waitTask = signal.WaitUntilAsync(wakeTime, cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // act
        signal.Notify(wakeTime);

        // assert
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(waitTask, completed);
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task WaitUntilAsync_Should_ReturnImmediately_When_NotifyRacedBeforeWait()
    {
        // arrange
        // a Notify that lowers the target arrives before the waiter calls WaitUntilAsync (the worker
        // read an empty or later next-due time, then an earlier message was scheduled). The pending
        // notify must not be lost: the next wait returns at once so the caller re-evaluates.
        var timeProvider = new FakeTimeProvider(s_baseTime);
        using var signal = new MessageBusSchedulerSignal(timeProvider);
        signal.Notify(s_baseTime.AddSeconds(1));

        // act - wait with a far-future wake time, as the worker would after seeing no due message
        var waitTask = signal.WaitUntilAsync(DateTimeOffset.MaxValue, CancellationToken.None);

        // assert - it returns without sleeping to the cap; time is never advanced in this test
        Assert.True(
            waitTask.IsCompleted,
            "WaitUntilAsync must not lose a notify that arrived before the wait");
        await waitTask;
    }
}
