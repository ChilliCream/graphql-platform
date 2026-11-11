using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQReplyQueueTests
{
    [Theory]
    [InlineData(1_000)]
    [InlineData(600_000)]
    public void ReplyQueue_Should_PreserveExplicitExpiry_When_TemporaryExpiryConfigured(int milliseconds)
    {
        // arrange
        var runtime = CreateRuntime(t => t.Endpoint("custom-replies")
            .Kind(ReceiveEndpointKind.Reply)
            .Temporary(TimeSpan.FromMilliseconds(milliseconds)));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var queue = transport.Topology.Queues.Single(q => q.Name == "custom-replies");

        // assert
        Assert.Equal(milliseconds, queue.Arguments["x-expires"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(RabbitMQQueueType.Classic)]
    [InlineData(RabbitMQQueueType.Stream)]
    public void ReplyQueue_Should_LeaveQueueTypeUnspecified_When_DefaultIsNotQuorum(string? queueType)
    {
        // arrange
        var runtime = CreateRuntime(t => t.ConfigureDefaults(d => d.Queue.QueueType = queueType));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name.StartsWith("response-", StringComparison.Ordinal));

        // assert
        Assert.True(queue.Durable);
        Assert.False(queue.Exclusive);
        Assert.False(queue.AutoDelete);
        Assert.Equal(new Dictionary<string, object?> { ["x-expires"] = 60000 }, queue.Arguments);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplyQueue_Should_InheritOnlyReplicationSettings_When_QuorumDefaultsConfigured(bool useArgument)
    {
        // arrange
        var runtime = CreateRuntime(t => t.ConfigureDefaults(d =>
        {
            if (useArgument)
            {
                d.Queue.Arguments["x-queue-type"] = RabbitMQQueueType.Quorum;
            }
            else
            {
                d.Queue.QueueType = RabbitMQQueueType.Quorum;
            }

            d.Queue.Durable = false;
            d.Queue.AutoDelete = true;
            d.Queue.Arguments["x-quorum-initial-group-size"] = 5;
            d.Queue.Arguments["x-delivery-limit"] = 5;
            d.Queue.Arguments["x-message-ttl"] = 1000;
            d.Queue.Arguments["x-expires"] = 1000;
        }));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name.StartsWith("response-", StringComparison.Ordinal));

        // assert
        Assert.True(queue.Durable);
        Assert.False(queue.Exclusive);
        Assert.False(queue.AutoDelete);
        Assert.Equal(
            new Dictionary<string, object?>
            {
                ["x-expires"] = 60000,
                ["x-queue-type"] = RabbitMQQueueType.Quorum,
                ["x-quorum-initial-group-size"] = 5
            },
            queue.Arguments);
    }

    private static MessagingRuntime CreateRuntime(Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        return services
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
    }
}
