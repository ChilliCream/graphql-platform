using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class PublishSubscribeTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public PublishSubscribeTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToHandler_When_SingleHandlerRegistered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-1", order.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_FanOutToAllHandlers_When_MultipleHandlersRegistered()
    {
        // arrange
        var recorder1 = new MessageRecorder();
        var recorder2 = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("r1", recorder1)
            .AddKeyedSingleton("r2", recorder2)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedKeyedHandler1>()
            .AddEventHandler<OrderCreatedKeyedHandler2>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder1.WaitAsync(s_timeout), "First handler did not receive the event");
        Assert.True(await recorder2.WaitAsync(s_timeout), "Second handler did not receive the event");

        Assert.Single(recorder1.Messages);
        Assert.Single(recorder2.Messages);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAll_When_MultipleEventsSequential()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-3" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 3),
            "Handler did not receive all 3 events within timeout");

        Assert.Equal(3, recorder.Messages.Count);

        var ids = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).OrderBy(id => id).ToList();

        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], ids);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverAll_When_RapidFire()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        const int messageCount = 50;

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not receive all {messageCount} events within timeout");

        Assert.Equal(messageCount, recorder.Messages.Count);

        var ids = recorder
            .Messages.Cast<OrderCreated>()
            .Select(m => m.OrderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(messageCount, ids.Distinct().Count());
    }

    public sealed class OrderCreatedKeyedHandler1([FromKeyedServices("r1")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OrderCreatedKeyedHandler2([FromKeyedServices("r2")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
