using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

/// <summary>
/// Verifies the unified Queue() front-door build-time behavior: an entity-only handle lowers to a
/// declared queue plus its BindFrom bindings without entering the receive-endpoint lifecycle,
/// satellites on an entity-only queue are a build error, two endpoints over one queue are a build
/// error, and a configuration that uses no Queue() handle is left byte-identical.
/// </summary>
public class RabbitMQQueueFrontDoorLoweringTests
{
    [Fact]
    public void Build_Should_Throw_When_TwoEndpointsShareOneQueue()
    {
        // arrange
        // Two distinct endpoints (named "a" and "b") are both pointed at the same backing queue
        // "shared". Each queue can host at most one receive endpoint, so the build must fail.
        void Build()
        {
            CreateRuntime(
                b => b.AddConsumer<OrderSpyConsumer>(),
                t =>
                {
                    t.BindHandlersExplicitly();
                    t.Endpoint("a").Queue("shared").Consumer<OrderSpyConsumer>();
                    t.Endpoint("b").Queue("shared").Consumer<OrderSpyConsumer>();
                });
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(Build);

        // assert
        Assert.Contains("shared", exception.Message);
        Assert.Contains("at most one receive endpoint", exception.Message);
    }

    [Fact]
    public void Build_Should_Throw_When_SatelliteConfiguredOnEntityOnlyQueue()
    {
        // arrange
        // An entity-only Queue() handle (no consumer, no Receives) cannot honor an error satellite
        // because no consumer processes failed messages, so configuring one is a build error.
        void Build()
        {
            CreateRuntime(
                b => { },
                t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("audit", q => q.ErrorQueue("audit_error"));
                });
        }

        // act
        var exception = Assert.Throws<InvalidOperationException>(Build);

        // assert
        Assert.Contains("audit", exception.Message);
        Assert.Contains("entity-only queue", exception.Message);
    }

    [Fact]
    public void Describe_Should_ShowEntityOnlyQueue_When_QueueWithoutConsumersOrReceives()
    {
        // arrange
        // An entity-only Queue() handle declares a queue and a BindFrom but never names a consumer.
        // It lowers to a declared queue plus a declared exchange-to-queue binding and produces no
        // receive endpoint (hence no satellites and no instance queue).
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("audit", q => q.BindFrom(new Uri("exchange:audit-events")));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert: no receive endpoint was materialized for the entity-only "audit" queue
        Assert.DoesNotContain(
            transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>(),
            e => e.Queue.Name == "audit");
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_ApplyQuorumArgument_When_EntityOnlyQueueIsQuorum()
    {
        // arrange
        // The unified handle's Quorum() shape verb sets x-queue-type on the lowered declared queue.
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("audit", q => q.Quorum());
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "audit");

        // assert
        Assert.Equal(RabbitMQQueueType.Quorum, queue.Arguments["x-queue-type"]);
    }

    [Fact]
    public void Describe_Should_StayByteIdentical_When_ConfigurationUsesNoQueueFrontDoor()
    {
        // arrange
        // A configuration declared entirely through Endpoint()/DeclareQueue() (no Queue() handle)
        // must flow through the partition code path unchanged.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders").AutoProvision(true);
                t.Endpoint("orders").Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
