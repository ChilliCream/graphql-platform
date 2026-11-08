using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

[Collection(RabbitMQCollections.Topology)]
public sealed class RabbitMQQueueProvisioningTests(RabbitMQFixture fixture)
{
    [Theory]
    [InlineData(null, RabbitMQQueueType.Quorum)]
    [InlineData(RabbitMQQueueType.Classic, RabbitMQQueueType.Classic)]
    [InlineData(RabbitMQQueueType.Quorum, RabbitMQQueueType.Quorum)]
    public async Task StartAsync_Should_UseExplicitOrBrokerQueueType_When_VirtualHostDefaultsToQuorum(
        string? queueType,
        string expectedType)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await fixture.InvokeCommandAsync(
            ["rabbitmqctl", "update_vhost_metadata", vhost.VhostName, "--default-queue-type", "quorum"]);

        // act
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                var queue = t.DeclareQueue("orders");
                if (queueType is not null)
                {
                    queue.QueueType(queueType);
                }
            })
            .BuildTestBusAsync();
        var output = await fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "-p", vhost.VhostName, "name", "type", "--formatter", "json", "--quiet"]);
        using var document = JsonDocument.Parse(output);
        var actualType = document.RootElement.EnumerateArray()
            .Single(queue => queue.GetProperty("name").GetString() == "orders").GetProperty("type").GetString();

        // assert
        Assert.Equal(expectedType, actualType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(RabbitMQQueueType.Classic)]
    public async Task StartAsync_Should_PreserveMessages_When_ExistingClassicQueueHasNoDeliveryLimit(string? queueType)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(true, true), TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", true, new BasicProperties(), "retained"u8.ToArray(),
            TestContext.Current.CancellationToken);

        // act
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                var queue = t.DeclareQueue("orders");
                if (queueType is not null)
                {
                    queue.QueueType(queueType);
                }
            })
            .BuildTestBusAsync();
        var message = await channel.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("retained", Encoding.UTF8.GetString(message!.Body.Span));
        Assert.Equal(0u, await channel.MessageCountAsync("orders", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, -1)]
    [InlineData(false, -1)]
    [InlineData(true, 5)]
    [InlineData(false, 5)]
    public async Task ProvisionAsync_Should_PreserveMessages_When_CombinedWithAutomaticProvisioning(
        bool provisionBeforeStart,
        int? deliveryLimit)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var provider = new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                var queue = t.DeclareQueue("orders").QueueType(RabbitMQQueueType.Quorum);
                if (deliveryLimit is { } limit)
                {
                    queue.MaxDeliveryLimit(limit);
                }
            })
            .Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var queue = transport.Topology.Queues.Single(queue => queue.Name == "orders");
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(true, true), TestContext.Current.CancellationToken);
        if (!provisionBeforeStart)
        {
            await runtime.StartAsync(TestContext.Current.CancellationToken);
        }

        // act
        await queue.ProvisionAsync(channel, connection, TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", true, new BasicProperties(), "order"u8.ToArray(),
            TestContext.Current.CancellationToken);
        if (provisionBeforeStart)
        {
            await runtime.StartAsync(TestContext.Current.CancellationToken);
        }

        await queue.ProvisionAsync(channel, connection, TestContext.Current.CancellationToken);
        var message = await channel.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);

        // assert
        Assert.True(channel.IsOpen);
        Assert.NotNull(message);
        Assert.Equal("order", Encoding.UTF8.GetString(message.Body.Span));
        Assert.Equal(0u, await channel.MessageCountAsync("orders", TestContext.Current.CancellationToken));
    }
}
