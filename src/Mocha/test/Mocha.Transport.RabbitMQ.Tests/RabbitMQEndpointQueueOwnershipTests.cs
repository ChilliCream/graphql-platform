using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies the Phase 1 endpoint-owns-queue invariant: the endpoint is the sole creator of its
/// backing queue, and a parallel <c>DeclareQueue</c> with the same name folds instead of throwing.
/// </summary>
public class RabbitMQEndpointQueueOwnershipTests
{
    [Fact]
    public void Describe_Should_StayByteIdentical_When_EndpointQueueUntouched()
    {
        // arrange
        // A consumer registered via implicit binding with no explicit DeclareQueue. The endpoint
        // creates the queue through OnDiscoverTopology; this snapshot is the regression anchor
        // proving the endpoint-owns-queue move is transparent to callers.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_NotThrow_When_DeclareQueueAndEndpointShareName()
    {
        // arrange
        // Both configurations target the same queue via the Queue() builder. The resulting
        // topology must contain exactly one queue and one set of satellite queues.
        var withDeclare = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });

        var withoutDeclare = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });

        var transportWithDeclare = withDeclare.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var transportWithoutDeclare = withoutDeclare.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var describeWithDeclare = transportWithDeclare.Describe();
        var describeWithoutDeclare = transportWithoutDeclare.Describe();

        // assert
        // Both configurations must produce a single "orders" queue. The DeclareQueue path
        // must not produce a duplicate or cause an exception.
        new Snapshot()
            .Add(RabbitMQDescribeSnapshot.Create(describeWithDeclare), "WithDeclareQueue", MarkdownLanguages.Json)
            .Add(RabbitMQDescribeSnapshot.Create(describeWithoutDeclare), "WithoutDeclareQueue", MarkdownLanguages.Json)
            .MatchMarkdown();
    }

    [Fact]
    public void EndpointQueue_Should_MergeViaProvenanceUpgrade_When_CollidesWithDeclareQueue()
    {
        // arrange
        // DeclareQueue adds the queue with Declared provenance and AutoProvision=true.
        // The endpoint then calls AddQueue unconditionally; AddQueue merges by identity
        // and the declared entity's provenance and properties win.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert: declared provenance and AutoProvision survive the endpoint merge
        Assert.Equal(RabbitMQTopologyProvenance.Declared, queue.Provenance);
        Assert.True(queue.AutoProvision);
    }

    [Fact]
    public void EndpointQueue_Should_UnionArguments_When_CollidesWithDeclareQueue()
    {
        // arrange
        // DeclareQueue adds a queue with a custom argument. The endpoint then calls
        // AddQueue for the same name; the merge must preserve the declared argument.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").WithArgument("x-dead-letter-exchange", "orders_dlx").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert: declared argument survives the endpoint merge
        Assert.True(queue.Arguments.ContainsKey("x-dead-letter-exchange"));
        Assert.Equal("orders_dlx", queue.Arguments["x-dead-letter-exchange"]);
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
