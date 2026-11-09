using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public sealed class TopologyRecoveryTests(RabbitMQFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_RestoreQueueAndUpstreamBindings_When_QueueIsDeleted(bool sendBeforeDeletion)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("orders");
                t.DeclareExchange("routed-orders").AutoDelete();
                t.DeclareQueue("archive");
                t.DeclareBinding("orders", "routed-orders").ToExchange("routed-orders");
                t.DeclareBinding("routed-orders", "archive");
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
                t.DispatchEndpoint("orders-send").ToQueue("archive").Send<OrderCreated>();
            })
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var dispatcher = bus.Provider.GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        var publisherConnection = await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "warmup" }, TestContext.Current.CancellationToken);
        if (sendBeforeDeletion)
        {
            await messageBus.SendAsync(new OrderCreated { OrderId = "warmup-send" }, TestContext.Current.CancellationToken);
        }

        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // No application consumer recreates the deleted queue, exchange, or bindings.
        await channel.QueueDeleteAsync("archive", cancellationToken: TestContext.Current.CancellationToken);
        await messageBus.SendAsync(new OrderCreated { OrderId = "direct" }, TestContext.Current.CancellationToken);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "published" }, TestContext.Current.CancellationToken);
        var orders = new List<(string? OrderId, string Exchange)>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        while (await channel.BasicGetAsync("archive", true, TestContext.Current.CancellationToken) is { } delivery)
        {
            var order = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span, options);
            orders.Add((order?.OrderId, delivery.Exchange));
        }

        // assert
        Assert.Equal([("direct", ""), ("published", "orders")], orders);
        Assert.Same(publisherConnection, await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublishAsync_Should_RestoreQueueAndBinding_When_PublisherReconnects()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("orders");
                t.DeclareQueue("archive");
                t.DeclareBinding("orders", "archive");
                t.DispatchEndpoint("orders").ToExchange("orders").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await vhost.DeleteQueueAsync("archive");

        // act
        // No application consumer recreates this queue or its binding.
        await fixture.CloseAllConnectionsAsync();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);

        // assert
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var delivery = await channel.BasicGetAsync("archive", true, TestContext.Current.CancellationToken);
        Assert.NotNull(delivery);
        var order = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("ORD-1", order?.OrderId);
        Assert.Equal("orders", delivery.Exchange);
    }
}
