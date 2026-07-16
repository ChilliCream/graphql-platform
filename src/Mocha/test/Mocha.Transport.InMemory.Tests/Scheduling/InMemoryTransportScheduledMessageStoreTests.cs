using Microsoft.Extensions.Time.Testing;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Scheduling;

namespace Mocha.Transport.InMemory.Tests.Scheduling;

public class InMemoryTransportScheduledMessageStoreTests
{
    private static readonly DateTimeOffset s_now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Add_Should_ReturnPrefixedToken_When_MessagePersisted()
    {
        // arrange
        using var store = CreateStore();

        // act
        var token = store.Add(CreateEnvelope("a"), s_now.AddMinutes(5));

        // assert
        Assert.StartsWith("in-memory-transport:", token);
    }

    [Fact]
    public void TryTakeDue_Should_ReturnFalse_When_EarliestNotDue()
    {
        // arrange
        using var store = CreateStore();
        store.Add(CreateEnvelope("a"), s_now.AddMinutes(5));

        // act
        var taken = store.TryTakeDue(s_now, out var entry);

        // assert
        Assert.False(taken);
        Assert.Null(entry);
    }

    [Fact]
    public void TryTakeDue_Should_ReturnEntriesInDueOrder_When_SameScheduledTime()
    {
        // arrange
        // two entries share a due time; the (ScheduledTime, Id) comparer must keep both.
        using var store = CreateStore();
        var due = s_now.AddMinutes(1);
        store.Add(CreateEnvelope("a"), due);
        store.Add(CreateEnvelope("b"), due);

        // act
        var first = store.TryTakeDue(due, out _);
        var second = store.TryTakeDue(due, out _);
        var third = store.TryTakeDue(due, out _);

        // assert
        Assert.True(first);
        Assert.True(second);
        Assert.False(third);
    }

    [Fact]
    public void NextDueTime_Should_ReturnEarliest_When_MultipleScheduled()
    {
        // arrange
        using var store = CreateStore();
        store.Add(CreateEnvelope("late"), s_now.AddMinutes(10));
        store.Add(CreateEnvelope("early"), s_now.AddMinutes(2));

        // act
        var next = store.NextDueTime();

        // assert
        Assert.Equal(s_now.AddMinutes(2), next);
    }

    [Fact]
    public async Task CancelAsync_Should_RemoveEntry_When_ValidToken()
    {
        // arrange
        using var store = CreateStore();
        var token = store.Add(CreateEnvelope("a"), s_now.AddMinutes(5));

        // act
        var cancelled = await store.CancelAsync(token, TestContext.Current.CancellationToken);

        // assert
        Assert.True(cancelled);
        Assert.Null(store.NextDueTime());
    }

    [Fact]
    public async Task CancelAsync_Should_ReturnFalse_When_UnknownToken()
    {
        // arrange
        using var store = CreateStore();

        // act
        var cancelled = await store.CancelAsync(
            $"in-memory-transport:{Guid.NewGuid():D}",
            TestContext.Current.CancellationToken);

        // assert
        Assert.False(cancelled);
    }

    [Fact]
    public void Add_Should_CopyBody_When_OriginalMutatedAfterPersist()
    {
        // arrange
        // the store must own its buffer; mutating the caller's array must not corrupt the entry.
        using var store = CreateStore();
        var body = new byte[] { 1, 2, 3 };
        var envelope = new MessageEnvelope { MessageType = "T", Body = body };

        // act
        store.Add(envelope, s_now);
        body[0] = 9;
        var taken = store.TryTakeDue(s_now, out var entry);

        // assert
        Assert.True(taken);
        Assert.NotNull(entry);
        Assert.Equal(new byte[] { 1, 2, 3 }, entry.Envelope.Body.ToArray());
    }

    private static InMemoryTransportScheduledMessageStore CreateStore()
        => new(new FakeTimeProvider(s_now));

    private static MessageEnvelope CreateEnvelope(string id)
        => new() { MessageId = id, MessageType = "T", Body = new byte[] { 0 } };
}
