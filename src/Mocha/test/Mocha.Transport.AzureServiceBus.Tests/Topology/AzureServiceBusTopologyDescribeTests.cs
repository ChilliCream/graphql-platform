using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.TestHelpers;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

/// <summary>
/// Snapshots the structural shape of <see cref="AzureServiceBusMessagingTransport.Describe"/> for
/// explicitly declared, implicitly discovered, and forwarding topology layouts.
/// </summary>
public class AzureServiceBusTopologyDescribeTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void Describe_Should_ShowDeclaredTopology_When_TopicQueueAndSubscriptionAreExplicit()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.DeclareTopic("orders");
            t.DeclareQueue("accounting");
            t.DeclareSubscription("orders", "accounting");
        });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - a topic, a queue, and the subscription linking them, all with declared origin.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_ShowConventionTopology_When_ConsumerBindsImplicitly()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - the publish topic, the consumer's endpoint queue, and the convention subscription
        // between them are all fabricated with convention origin.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitConventionTopology_When_ConsumerBindsExplicitly()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - only the declared queue remains; no fabricated topic or subscription appears
        // because explicit binding suppresses discovery.
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DeclareQueue_Should_RetainForwardTo_When_QueueForwardsToAnotherQueue()
    {
        // arrange
        var (_, transport, topology) = CreateTopology(t =>
        {
            t.DeclareQueue("staging").ForwardTo("archive");
            t.DeclareQueue("archive");
        });

        // act
        var staging = topology.Queues.Single(q => q.Name == "staging");
        var archive = topology.Queues.Single(q => q.Name == "archive");
        var description = transport.Describe();

        // assert - forwarding is a plain queue attribute, not a topology link, so both queues exist
        // as independent entities and only the source queue carries the forward target. Describe()
        // does not emit ForwardTo, so the snapshot below pins the two-queue Describe() shape and the
        // Assert.Equal/Assert.Null pair above cover the forward target itself.
        Assert.Equal("archive", staging.ForwardTo);
        Assert.Null(archive.ForwardTo);
        AzureServiceBusDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static (
        MessagingRuntime Runtime,
        AzureServiceBusMessagingTransport Transport,
        AzureServiceBusMessagingTopology Topology) CreateTopology(
        Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var runtime = CreateRuntime(configure);
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        return (runtime, transport, (AzureServiceBusMessagingTopology)transport.Topology);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IAzureServiceBusMessagingTransportDescriptor> configureTransport)
        => CreateRuntime(_ => { }, configureTransport);

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IAzureServiceBusMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configureTransport(t);
            })
            .BuildRuntime();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
