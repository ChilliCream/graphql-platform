using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public sealed class TopologyRecoveryTests(RabbitMQFixture fixture)
{
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
