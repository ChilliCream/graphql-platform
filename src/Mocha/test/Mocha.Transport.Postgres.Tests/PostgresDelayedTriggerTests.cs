using Mocha.Threading;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresDelayedTriggerTests
{
    [Fact]
    public async Task DelayedTrigger_Should_SetSignal_When_ScheduledTimeArrives()
    {
        // arrange
        using var signal = new AsyncAutoResetEvent();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(50);

        await using var trigger = new PostgresDelayedTrigger(scheduledAt, signal);

        // act
        await signal.WaitAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        // assert
        Assert.True(trigger.IsSet);
    }

    [Fact]
    public async Task DelayedTrigger_Should_NotSetSignal_When_DisposedBeforeScheduledTime()
    {
        // arrange
        using var signal = new AsyncAutoResetEvent();
        var scheduledAt = DateTimeOffset.UtcNow.AddSeconds(30);

        var trigger = new PostgresDelayedTrigger(scheduledAt, signal);

        // act
        await trigger.DisposeAsync();

        // assert
        Assert.False(trigger.IsSet);
    }

    [Fact]
    public void ScheduledAt_Should_ReturnConfiguredTime_When_Created()
    {
        // arrange
        using var signal = new AsyncAutoResetEvent();
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // act
        var trigger = new PostgresDelayedTrigger(scheduledAt, signal);

        // assert
        Assert.Equal(scheduledAt, trigger.ScheduledAt);

        // cleanup
        _ = trigger.DisposeAsync();
    }

    [Fact]
    public async Task DelayedTrigger_Should_SetSignalImmediately_When_ScheduledTimeInPast()
    {
        // arrange
        using var signal = new AsyncAutoResetEvent();
        var scheduledAt = DateTimeOffset.UtcNow.AddMilliseconds(-100);

        await using var trigger = new PostgresDelayedTrigger(scheduledAt, signal);

        // act
        await signal.WaitAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        // assert
        Assert.True(trigger.IsSet);
    }

    [Fact]
    public void IsSet_Should_ReturnFalse_When_JustCreated()
    {
        // arrange
        using var signal = new AsyncAutoResetEvent();
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // act
        var trigger = new PostgresDelayedTrigger(scheduledAt, signal);

        // assert
        Assert.False(trigger.IsSet);

        // cleanup
        _ = trigger.DisposeAsync();
    }
}
