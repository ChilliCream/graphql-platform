using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQAddressResolutionTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("tenant-a")]
    public void GetDispatchEndpoint_Should_ResolveQueue_When_AddressIsTopologyAddress(string virtualHost)
    {
        // arrange
        var (runtime, topology) = CreateRuntime(virtualHost);
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // act
        var endpoint = runtime.GetDispatchEndpoint(queue.Address);

        // assert
        Assert.Equal("q/orders", endpoint.Name);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("tenant-a")]
    public void GetDispatchEndpoint_Should_ResolveExchange_When_AddressIsTopologyAddress(string virtualHost)
    {
        // arrange
        var (runtime, topology) = CreateRuntime(virtualHost);
        var exchange = topology.Exchanges.Single(e => e.Name == "events");

        // act
        var endpoint = runtime.GetDispatchEndpoint(exchange.Address);

        // assert
        Assert.Equal("e/events", endpoint.Name);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ResolveQueue_When_AddressOmitsHostAndVirtualHost()
    {
        // arrange
        // the short form used for fault and skipped endpoint addresses
        var (runtime, _) = CreateRuntime("tenant-a");

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:q/orders"));

        // assert
        Assert.Equal("q/orders", endpoint.Name);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("tenant-a")]
    public void Topology_Should_RegisterFaultQueue_When_FaultAddressIsTopologyAddress(string virtualHost)
    {
        // arrange
        var faultAddress = virtualHost is "/"
            ? "rabbitmq://localhost:5672/q/orders-errors"
            : $"rabbitmq://localhost:5672/{virtualHost}/q/orders-errors";

        // act
        var (runtime, topology) = CreateRuntime(
            virtualHost,
            t => t.Queue("orders").Handler<OrderCreatedHandler>().FaultEndpoint(new Uri(faultAddress)));

        // assert
        Assert.Contains(topology.Queues, q => q.Name == "orders-errors");
    }

    [Fact]
    public void GetDispatchEndpoint_Should_Throw_When_AddressIsOnAnotherVirtualHost()
    {
        // arrange
        var (runtime, _) = CreateRuntime("tenant-a");
        var address = new Uri("rabbitmq://localhost:5672/other/q/orders");

        // act
        var exception = Record.Exception(() => runtime.GetDispatchEndpoint(address));

        // assert
        Assert.Equal(
            "No transport can handle address: rabbitmq://localhost:5672/other/q/orders",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    private static (MessagingRuntime Runtime, RabbitMQMessagingTopology Topology) CreateRuntime(
        string virtualHost,
        Action<IRabbitMQMessagingTransportDescriptor>? configureTransport = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();

        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new VirtualHostConnectionProvider(virtualHost));
                t.DeclareQueue("orders");
                t.DeclareExchange("events");
                configureTransport?.Invoke(t);
            })
            .BuildRuntime();

        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        return (runtime, (RabbitMQMessagingTopology)transport.Topology);
    }

    private sealed class VirtualHostConnectionProvider(string virtualHost) : IRabbitMQConnectionProvider
    {
        public string Host => "localhost";

        public string VirtualHost => virtualHost;

        public int Port => 5672;

        public ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("This provider does not create real connections.");
    }
}
