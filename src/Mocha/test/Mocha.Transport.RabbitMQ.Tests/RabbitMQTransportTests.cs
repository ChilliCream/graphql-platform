using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQTransportTests
{
    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveQueue_When_DestinationAddressUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var queueEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is RabbitMQQueue);
        Assert.NotNull(queueEndpoint);

        var destinationAddress = queueEndpoint.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve queue destination address");
        Assert.NotNull(endpoint);
        Assert.IsType<RabbitMQQueue>(endpoint.Destination);
        Assert.Same(queueEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveExchange_When_DestinationAddressUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var exchangeEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is RabbitMQExchange);
        Assert.NotNull(exchangeEndpoint);

        var destinationAddress = exchangeEndpoint.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve exchange destination address");
        Assert.NotNull(endpoint);
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
        Assert.Same(exchangeEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_RabbitMQSchemeMatchesAddress()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var existingEndpoint = transport.DispatchEndpoints.First();
        var address = existingEndpoint.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(address, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve matching address");
        Assert.NotNull(endpoint);
        Assert.Same(existingEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnReplyDispatchEndpoint_When_ReplyAliasUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var found = transport.TryGetDispatchEndpoint(new Uri("rabbitmq:replies"), out var endpoint);

        // assert
        Assert.True(found);
        Assert.Same(transport.ReplyDispatchEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_TopologyBaseAddressUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = transport.Topology;

        var dispatchEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            topology.Address.IsBaseOf(e.Destination.Address)
        );
        Assert.NotNull(dispatchEndpoint);

        var destinationAddress = dispatchEndpoint.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve topology-based address");
        Assert.NotNull(endpoint);
        Assert.Same(dispatchEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_UnknownUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var unknownUri = new Uri("http://unknown-host/nonexistent");

        // act
        var found = transport.TryGetDispatchEndpoint(unknownUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for unknown URI");
        Assert.Null(endpoint);
    }

    [Fact]
    public void Describe_Should_ListExchangesQueuesAndBindings_When_EventHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_SendRoute()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var queueEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination is RabbitMQQueue && e.Kind == DispatchEndpointKind.Default
        );

        // assert
        Assert.NotNull(queueEndpoint);
        Assert.StartsWith("q/", queueEndpoint.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateExchangeConfig_When_PublishRoute()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var exchangeEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is RabbitMQExchange);

        // assert
        Assert.NotNull(exchangeEndpoint);
        Assert.StartsWith("e/", exchangeEndpoint.Name);
    }

    [Fact]
    public void Schema_Should_BeRabbitMQ_When_TransportCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.Equal("rabbitmq", transport.Schema);
    }

    [Fact]
    public void Convention_Should_SetErrorAndSkippedEndpoints_When_DefaultEndpoint()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        var faultFeature = receiveEndpoint.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.NotNull(faultFeature?.Endpoint);
        Assert.Contains("_error", faultFeature.Endpoint.Name);

        var skippedFeature = receiveEndpoint.Features.Get<ReceiveSkippedEndpointFeature>();
        Assert.NotNull(skippedFeature?.Endpoint);
        Assert.Contains("_skipped", skippedFeature.Endpoint.Name);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveQueue_When_QueueSchemeUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var queueEndpoint = transport.DispatchEndpoints.First(e => e.Destination is RabbitMQQueue);
        var queueName = ((RabbitMQQueue)queueEndpoint.Destination).Name;

        // act
        var found = transport.TryGetDispatchEndpoint(new Uri("queue:" + queueName), out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve queue: scheme URI");
        Assert.NotNull(endpoint);
        Assert.Same(queueEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveExchange_When_ExchangeSchemeUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        var exchangeEndpoint = transport.DispatchEndpoints.First(e => e.Destination is RabbitMQExchange);
        var exchangeName = ((RabbitMQExchange)exchangeEndpoint.Destination).Name;

        // act
        var found = transport.TryGetDispatchEndpoint(new Uri("exchange:" + exchangeName), out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve exchange: scheme URI");
        Assert.NotNull(endpoint);
        Assert.Same(exchangeEndpoint, endpoint);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_QueueSchemeUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("queue:my-queue"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", rabbitConfig.QueueName);
        Assert.Equal("q/my-queue", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateExchangeConfig_When_ExchangeSchemeUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("exchange:my-exchange"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-exchange", rabbitConfig.ExchangeName);
        Assert.Equal("e/my-exchange", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_RabbitMQSchemeQueuePath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("rabbitmq:///q/my-queue"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", rabbitConfig.QueueName);
        Assert.Equal("q/my-queue", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateExchangeConfig_When_RabbitMQSchemeExchangePath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("rabbitmq:///e/my-exchange"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-exchange", rabbitConfig.ExchangeName);
        Assert.Equal("e/my-exchange", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateReplyConfig_When_InternalRabbitMQReplyAddressUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("rabbitmq:replies"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal(DispatchEndpointKind.Reply, rabbitConfig.Kind);
        Assert.Equal("Replies", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateQueueConfig_When_TopologyBaseQueueUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = transport.Topology.Address;

        // act - use the full topology base address with q/ path
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "q/my-queue"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-queue", rabbitConfig.QueueName);
        Assert.Equal("q/my-queue", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateExchangeConfig_When_TopologyBaseExchangeUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = transport.Topology.Address;

        // act - use the full topology base address with e/ path
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "e/my-exchange"));

        // assert
        Assert.NotNull(config);
        var rabbitConfig = Assert.IsType<RabbitMQDispatchEndpointConfiguration>(config);
        Assert.Equal("my-exchange", rabbitConfig.ExchangeName);
        Assert.Equal("e/my-exchange", rabbitConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnNull_When_UnknownScheme()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("http://foo/bar"));

        // assert
        Assert.Null(config);
    }

    [Fact]
    public async Task DisposeAsync_Should_NotThrow_When_TransportNeverInitialized()
    {
        // arrange
        var transport = new RabbitMQMessagingTransport(_ => { });

        // act
        var exception = await Record.ExceptionAsync(() => transport.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.AddRabbitMQ(t => t.ConnectionProvider(_ => new StubConnectionProvider())).BuildRuntime();
        return runtime;
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
