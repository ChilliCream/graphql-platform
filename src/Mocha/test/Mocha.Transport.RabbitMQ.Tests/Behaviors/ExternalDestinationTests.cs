using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// Covers address-based sends to queues another service owns: the sending host binds explicitly,
/// never declares the queue, and still has to reach it on the broker.
/// </summary>
[Collection(RabbitMQCollections.Topology)]
public class ExternalDestinationTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task SendAsync_Should_DeliverToForeignQueue_When_QueueNotDeclaredUnderExplicitBinding()
    {
        // arrange
        // the queue belongs to another service: it exists on the broker and this host never declares it
        const string queueName = "reporting.events";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t => t.BindExplicitly())
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await vhost.SetConfigurePermissionAsync("^$");
        try
        {
            await messageBus.SendAsync(
                new OrderCreated { OrderId = "EXTERNAL-1" },
                new SendOptions { Endpoint = new Uri($"rabbitmq:q/{queueName}") },
                cancellationToken);
        }
        finally
        {
            await vhost.SetConfigurePermissionAsync(".*");
        }

        // assert
        var delivery = await GetMessageAsync(channel, queueName, cancellationToken);
        Assert.NotNull(delivery);
        Assert.Equal("", delivery.Exchange);
        Assert.Equal(queueName, delivery.RoutingKey);
        var order = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("EXTERNAL-1", order?.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowWithoutDeclaringQueue_When_QueueNotDeclaredUnderExplicitBinding()
    {
        // arrange
        const string queueName = "reporting.missing";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t => t.BindExplicitly())
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        // the broker returns a message sent to a missing queue instead of dropping it
        var sendException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await messageBus.SendAsync(
            new OrderCreated { OrderId = "EXTERNAL-2" },
            new SendOptions { Endpoint = new Uri($"rabbitmq:q/{queueName}") },
            cancellationToken));

        // assert
        // a passive declare only succeeds for a queue that exists, so a 404 proves the send
        // did not declare the queue it addressed
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var exception = await Assert.ThrowsAsync<OperationInterruptedException>(
            () => channel.QueueDeclarePassiveAsync(queueName, cancellationToken));
        Assert.IsType<PublishReturnException>(sendException.InnerException);
        Assert.Equal<ushort?>(404, exception.ShutdownReason?.ReplyCode);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToForeignExchange_When_SenderCannotDeclareResources()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync("reporting", "fanout", durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("reports", durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync("reports", "reporting", "", cancellationToken: cancellationToken);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t => t.BindExplicitly())
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await vhost.SetConfigurePermissionAsync("^$");
        try
        {
            await messageBus.SendAsync(
                new OrderCreated { OrderId = "EXTERNAL-3" },
                new SendOptions { Endpoint = new Uri("rabbitmq:e/reporting") },
                cancellationToken);
        }
        finally
        {
            await vhost.SetConfigurePermissionAsync(".*");
        }

        // assert
        var delivery = await GetMessageAsync(channel, "reports", cancellationToken);
        Assert.NotNull(delivery);
        var order = JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("EXTERNAL-3", order?.OrderId);
        Assert.Equal("reporting", delivery.Exchange);
    }

    private static async Task<BasicGetResult?> GetMessageAsync(
        IChannel channel,
        string queueName,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + s_timeout;

        while (true)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken);
            if (result is not null || DateTime.UtcNow >= deadline)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }
}
