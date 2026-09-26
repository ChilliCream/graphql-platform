using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQReplyQueueTests
{
    [Fact]
    public void ReplyQueue_Should_BeDurableAutoDeleteWithExpiry_When_RequestHandlerRegistered()
    {
        // arrange
        // RabbitMQ 4.3 denies non-durable, non-exclusive queues by default, so the per-instance
        // reply queue must stay durable and rely on auto-delete plus a queue expiry.
        var runtime = CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name.StartsWith("response-", StringComparison.Ordinal));

        // assert
        new { queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments }.MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {
                "x-expires": 1800000
              }
            }
            """);
    }

    [Fact]
    public void ReplyQueue_Should_StayClassic_When_QuorumDefaultsConfigured()
    {
        // arrange
        // Quorum queues reject auto-delete, so bus defaults must not reach the reply queue.
        var runtime = CreateRuntime(t => t.ConfigureDefaults(d =>
        {
            d.Queue.QueueType = RabbitMQQueueType.Quorum;
            d.Queue.Arguments["x-delivery-limit"] = 5;
        }));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name.StartsWith("response-", StringComparison.Ordinal));

        // assert
        new { queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments }.MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {
                "x-expires": 1800000
              }
            }
            """);
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
