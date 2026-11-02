using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

/// <summary>
/// Verifies the unified Queue() API build-time behavior and that a configuration
/// that uses no Queue() API is left byte-identical.
/// </summary>
public class RabbitMQQueueLoweringTests
{
    [Fact]
    public void Queue_Should_ConfigureFaultEndpoint_When_NoConsumerAttached()
    {
        // arrange
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit").FaultEndpoint(new Uri("queue:audit_error"));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .Single(e => e.Queue.Name == "audit");
        var feature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();

        // assert
        Assert.Equal("queue:audit_error", feature?.Address?.OriginalString);
        Assert.False(feature?.IsDisabled ?? false);
    }

    [Fact]
    public void Queue_Should_MaterializeReceiveEndpoint_When_NoConsumersOrReceives()
    {
        // arrange
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "audit");

        // assert
        Assert.NotNull(endpoint);
    }

    [Fact]
    public void Describe_Should_ApplyQuorumArgument_When_QueueIsQuorum()
    {
        // arrange
        // The Queue() descriptor's Quorum() shape verb sets x-queue-type on the declared queue.
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
    public void Describe_Should_StayByteIdentical_When_ConfigurationUsesNoUnifiedQueueApi()
    {
        // arrange
        // A configuration declared entirely through DeclareQueue()/Endpoint() (no Queue() API)
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
