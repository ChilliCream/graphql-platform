using System.Buffers;
using System.Text;
using HotChocolate.Fusion.Execution.Brokers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Brokers;

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
            .Subscribe("book.created", cts.Token)
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

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
    }
}
