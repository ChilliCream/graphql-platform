using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public sealed class ReplyQueueCleanupTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_DeleteReplyQueue_When_QueueIsGenerated()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var queue = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>()
            .Single(endpoint => endpoint.Kind == ReceiveEndpointKind.Reply).Queue;
        var beforeStop = await vhost.GetConsumerCountAsync(queue.Name);

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        var afterStop = await vhost.GetConsumerCountAsync(queue.Name);

        // assert
        new { BeforeStop = beforeStop, AfterStop = afterStop }.MatchInlineSnapshot(
            """
            {
              "BeforeStop": 1,
              "AfterStop": null
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_Should_PreserveReplyQueue_When_QueueIsPersistent(bool autoProvision)
    {
        // arrange
        const string queueName = "persistent-replies";
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue(queueName).AutoProvision(autoProvision);
                t.Endpoint(queueName).Kind(ReceiveEndpointKind.Reply);
            })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var beforeStop = await vhost.GetConsumerCountAsync(queueName);

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        var afterStop = await vhost.GetConsumerCountAsync(queueName);

        // assert
        new { BeforeStop = beforeStop, AfterStop = afterStop }.MatchInlineSnapshot(
            """
            {
              "BeforeStop": 1,
              "AfterStop": 0
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_PreserveTemporaryReplyQueue_When_AutoProvisionIsDisabled()
    {
        // arrange
        const string queueName = "external-replies";
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-expires"] = 60000 },
            cancellationToken: TestContext.Current.CancellationToken);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.AutoProvision(false);
                t.Endpoint(queueName).Kind(ReceiveEndpointKind.Reply).Temporary();
            })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var beforeStop = await vhost.GetConsumerCountAsync(queueName);

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        var afterStop = await vhost.GetConsumerCountAsync(queueName);

        // assert
        new { BeforeStop = beforeStop, AfterStop = afterStop }.MatchInlineSnapshot(
            """
            {
              "BeforeStop": 1,
              "AfterStop": 0
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_PreserveDeclaredReplyQueue_When_AnotherConsumerIsAttached()
    {
        // arrange
        const string queueName = "shared-replies";
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            queueName, durable: true, exclusive: false, autoDelete: true,
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.BasicConsumeAsync(
            queueName, autoAck: true, consumer: new AsyncDefaultBasicConsumer(channel),
            cancellationToken: TestContext.Current.CancellationToken);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue(queueName).AutoDelete();
                t.Endpoint(queueName).Kind(ReceiveEndpointKind.Reply).Temporary();
            })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var beforeStop = await vhost.GetConsumerCountAsync(queueName);

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        var afterStop = await vhost.GetConsumerCountAsync(queueName);

        // assert
        new { BeforeStop = beforeStop, AfterStop = afterStop }.MatchInlineSnapshot(
            """
            {
              "BeforeStop": 2,
              "AfterStop": 1
            }
            """);
    }
}
