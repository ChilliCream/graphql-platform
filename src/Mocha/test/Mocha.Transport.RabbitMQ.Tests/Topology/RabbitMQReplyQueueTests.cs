using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQReplyQueueTests
{
    [Fact]
    public void ReplyQueue_Should_BeDurableExpiringQueue_When_RequestHandlerRegistered()
    {
        // arrange
        // RabbitMQ 4.3 denies non-durable, non-exclusive queues by default, so the per-instance
        // reply queue stays durable and expires instead of being deleted with its last consumer.
        var runtime = CreateRuntime(_ => { });
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

    [Fact]
    public void ReplyQueue_Should_StayClassic_When_QuorumDefaultsConfigured()
    {
        // arrange
        // The reply queue is per instance and short-lived, so bus defaults must not reach it.
        var runtime = CreateRuntime(t => t.ConfigureDefaults(d =>
        {
            d.Queue.QueueType = RabbitMQQueueType.Quorum;
            d.Queue.Arguments["x-delivery-limit"] = 5;
        }));
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
