using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

/// <summary>
/// Verifies the unified Queue() front-door build-time behavior: an entity-only handle lowers to a
/// declared queue without entering the receive-endpoint lifecycle, error and skipped queues on an entity-only queue
/// are a build error, and a configuration that uses no Queue() handle is left byte-identical.
/// </summary>
public class RabbitMQQueueFrontDoorLoweringTests
{
    [Fact]
    public void Build_Should_Throw_When_ErrorQueueConfiguredOnEntityOnlyQueue()
    {
        // arrange
        // An entity-only Queue() handle (no consumer, no Receives) cannot honor an error queue
        // because no consumer processes failed messages, so configuring one is a build error.
        void Build()
        {
            CreateRuntime(
                b => { },
                t =>
                {
                    t.BindExplicitly();
                    t.Queue("audit").ErrorQueue("audit_error");
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
        // An entity-only Queue() handle (no consumer, no Receives) lowers to a declared queue and
        // produces no receive endpoint, error/skipped queues, or instance queue.
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit");
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
                t.BindExplicitly();
                t.Queue("audit").Quorum();
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
        // A configuration declared entirely through DeclareQueue()/Endpoint() (no Queue() handle)
        // must flow through the build code path unchanged.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoProvision(true);
                t.Endpoint("orders").Consumer<OrderSpyConsumer>();
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
