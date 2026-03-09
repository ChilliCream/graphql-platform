using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

public class FaultHandlingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public FaultHandlingTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestAsync_Should_ThrowRemoteError_When_HandlerThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ThrowingRequestHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert
        var ex = await Assert.ThrowsAsync<RemoteErrorException>(async () =>
            await messageBus.RequestAsync(new GetOrderStatus { OrderId = "ORD-FAIL" }, CancellationToken.None)
        );

        Assert.Contains("InvalidOperationException", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_Should_NotAffectOtherHandlers_When_OneHandlerThrows()
    {
        // arrange
        var throwingRecorder = new MessageRecorder();
        var normalRecorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("throwing", throwingRecorder)
            .AddKeyedSingleton("shipment", normalRecorder)
            .AddMessageBus()
            .AddEventHandler<ThrowingEventHandler>()
            .AddEventHandler<ItemShippedKeyedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish event that triggers a throw
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);

        // wait a bit for the throwing handler to process
        await throwingRecorder.WaitAsync(TimeSpan.FromSeconds(2));

        // now publish a normal event
        await messageBus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-1" }, CancellationToken.None);

        // assert - the second handler still works
        Assert.True(
            await normalRecorder.WaitAsync(s_timeout),
            "Normal handler did not receive event after a previous handler threw");
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
