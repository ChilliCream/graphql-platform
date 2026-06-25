using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.InMemory.Tests.Scheduling;

public class InMemoryScheduledMessageStoreTests
{
    [Fact]
    public async Task PersistAsync_Should_ReturnPrefixedToken_When_Called()
    {
        // arrange
        var store = new InMemoryScheduledMessageStore(new NoopSignal());

        // act
        var token = await store.PersistAsync(
            Envelope("a"),
            DateTimeOffset.UtcNow.AddMinutes(1),
            TestContext.Current.CancellationToken);

        // assert
        Assert.StartsWith("in-memory-scheduler:", token);
    }

    [Fact]
    public async Task TryTakeDue_Should_ReturnEarliestDue_When_MultiplePending()
    {
        // arrange
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryScheduledMessageStore(new NoopSignal());
        await store.PersistAsync(Envelope("late"), now.AddMinutes(10), TestContext.Current.CancellationToken);
        await store.PersistAsync(Envelope("early"), now.AddMinutes(1), TestContext.Current.CancellationToken);

        // act
        var took = store.TryTakeDue(now.AddMinutes(2), out var envelope);

        // assert
        Assert.True(took);
        Assert.NotNull(envelope);
        Assert.Equal("early", envelope.MessageId);
    }

    [Fact]
    public async Task TryTakeDue_Should_ReturnFalse_When_NothingDue()
    {
        // arrange
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryScheduledMessageStore(new NoopSignal());
        await store.PersistAsync(Envelope("future"), now.AddMinutes(10), TestContext.Current.CancellationToken);

        // act
        var took = store.TryTakeDue(now, out _);

        // assert
        Assert.False(took);
    }

    [Fact]
    public async Task CancelAsync_Should_RemoveEntry_When_TokenValid()
    {
        // arrange
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryScheduledMessageStore(new NoopSignal());
        var token = await store.PersistAsync(Envelope("x"), now.AddMinutes(1), TestContext.Current.CancellationToken);

        // act
        var cancelled = await store.CancelAsync(token, TestContext.Current.CancellationToken);

        // assert
        Assert.True(cancelled);
        Assert.False(store.TryTakeDue(now.AddMinutes(2), out _));
    }

    [Fact]
    public async Task CancelAsync_Should_ReturnFalse_When_TokenUnknown()
    {
        // arrange
        var store = new InMemoryScheduledMessageStore(new NoopSignal());

        // act
        var cancelled = await store.CancelAsync(
            "in-memory-scheduler:00000000-0000-0000-0000-000000000000",
            TestContext.Current.CancellationToken);

        // assert
        Assert.False(cancelled);
    }

    private static MessageEnvelope Envelope(string id)
        => new() { MessageId = id, MessageType = "urn:test", DestinationAddress = "memory://test" };

    private sealed class NoopSignal : ISchedulerSignal
    {
        public void Notify(DateTimeOffset scheduledTime) { }

        public Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
