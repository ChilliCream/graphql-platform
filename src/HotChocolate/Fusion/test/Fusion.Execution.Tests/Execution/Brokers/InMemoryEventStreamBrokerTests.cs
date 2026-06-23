using System.Buffers;
using System.Text;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class InMemoryEventStreamBrokerTests
{
    [Fact]
    public async Task Subscribe_Should_ReadPublishedMessages_When_DefaultBrokerIsRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddInMemoryEventStreamBroker();

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        var publisher = provider.GetRequiredService<IInMemoryEventStreamPublisher>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var enumerator = broker
            .Subscribe(
                EmptySubscriptionFieldContext.Instance,
                ["book.created"],
                cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var next = enumerator.MoveNextAsync().AsTask();
        await Task.Yield();

        // act
        await publisher.PublishAsync(
            "book.created",
            CreateMessage("""{"id":1}"""u8),
            cts.Token);

        var hasMessage = await next;

        // assert
        Assert.True(hasMessage);
        using var message = enumerator.Current;
        Assert.Equal("""{"id":1}""", Encoding.UTF8.GetString(message.Body));
    }

    [Fact]
    public async Task Subscribe_Should_ReadPublishedMessages_When_MultipleTopicsAreRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddInMemoryEventStreamBroker();

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IEventStreamBrokerFactory>();
        var publisher = provider.GetRequiredService<IInMemoryEventStreamPublisher>();
        await using var broker = factory.Create(null);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var enumerator = broker
            .Subscribe(
                EmptySubscriptionFieldContext.Instance,
                ["book.created", "book.updated"],
                cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var first = enumerator.MoveNextAsync().AsTask();
        await Task.Yield();

        // act
        await publisher.PublishAsync(
            "book.created",
            CreateMessage("""{"id":1}"""u8),
            cts.Token);

        var hasFirstMessage = await first;
        using var firstMessage = enumerator.Current;
        var firstBody = Encoding.UTF8.GetString(firstMessage.Body);

        var second = enumerator.MoveNextAsync().AsTask();
        await Task.Yield();

        await publisher.PublishAsync(
            "book.updated",
            CreateMessage("""{"id":2}"""u8),
            cts.Token);

        var hasSecondMessage = await second;

        // assert
        Assert.True(hasFirstMessage);
        Assert.Equal("""{"id":1}""", firstBody);
        Assert.True(hasSecondMessage);
        using var secondMessage = enumerator.Current;
        Assert.Equal("""{"id":2}""", Encoding.UTF8.GetString(secondMessage.Body));
    }

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
    }

    private sealed class EmptySubscriptionFieldContext : ISubscriptionFieldContext
    {
        public static readonly EmptySubscriptionFieldContext Instance = new();

        private EmptySubscriptionFieldContext()
        {
        }

        public IReadOnlyDictionary<string, IValueNode> Arguments { get; } =
            new Dictionary<string, IValueNode>();
    }
}
