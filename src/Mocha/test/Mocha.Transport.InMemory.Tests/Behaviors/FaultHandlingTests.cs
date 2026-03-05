using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class FaultHandlingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_NotAffectOtherHandlers_When_OneHandlerThrows()
    {
        // arrange
        var throwingRecorder = new MessageRecorder();
        var normalRecorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddKeyedSingleton("throwing", throwingRecorder)
            .AddKeyedSingleton("shipment", normalRecorder)
            .AddMessageBus()
            .AddEventHandler<ThrowingEventHandler>()
            .AddEventHandler<ItemShippedKeyedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish event that triggers a throw
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);

        // wait a bit for the throwing handler to process
        await throwingRecorder.WaitAsync(TimeSpan.FromSeconds(2));

        // now publish a normal event
        await bus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-1" }, CancellationToken.None);

        // assert - the second handler still works
        Assert.True(
            await normalRecorder.WaitAsync(s_timeout),
            "Normal handler did not receive event after a previous handler threw");
    }

    [Fact]
    public async Task RequestAsync_Should_ThrowRemoteError_When_HandlerThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ThrowingRequestHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert — fault middleware sends NotAcknowledgedEvent back to the caller,
        // which surfaces as RemoteErrorException
        var ex = await Assert.ThrowsAsync<RemoteErrorException>(async () =>
            await bus.RequestAsync(new GetOrderStatus { OrderId = "ORD-FAIL" }, CancellationToken.None)
        );

        Assert.Contains("InvalidOperationException", ex.Message);
    }

    public sealed class ItemShipped
    {
        public required string TrackingNumber { get; init; }
    }

    public sealed class ThrowingEventHandler([FromKeyedServices("throwing")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }

    public sealed class ItemShippedKeyedHandler([FromKeyedServices("shipment")] MessageRecorder recorder)
        : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class ThrowingRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(GetOrderStatus request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            throw new InvalidOperationException("Request handler failed deliberately");
        }
    }
}
