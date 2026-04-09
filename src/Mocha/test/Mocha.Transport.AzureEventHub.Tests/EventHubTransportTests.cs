using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests;

public class EventHubTransportTests
{
    [Fact]
    public void CreateEndpointConfiguration_Should_CreateHubConfig_When_SchemeRelativeHubPath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("eventhub:///h/my-hub"));

        // assert
        Assert.NotNull(config);
        var ehConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(config);
        Assert.Equal("my-hub", ehConfig.HubName);
        Assert.Equal("h/my-hub", ehConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateHubConfig_When_FullNamespaceAddress()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = ((EventHubMessagingTopology)transport.Topology).Address;

        // act - use the full topology base address with h/ path
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "h/my-hub"));

        // assert
        Assert.NotNull(config);
        var ehConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(config);
        Assert.Equal("my-hub", ehConfig.HubName);
        Assert.Equal("h/my-hub", ehConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateHubConfig_When_HubShorthandScheme()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("hub://my-hub"));

        // assert
        Assert.NotNull(config);
        var ehConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(config);
        Assert.Equal("my-hub", ehConfig.HubName);
        Assert.Equal("h/my-hub", ehConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateReplyConfig_When_EventHubRepliesPath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("eventhub:///replies"));

        // assert
        Assert.NotNull(config);
        var ehConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(config);
        Assert.Equal(DispatchEndpointKind.Reply, ehConfig.Kind);
        Assert.Equal("Replies", ehConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnNull_When_UnknownScheme()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("http://foo/bar"));

        // assert
        Assert.Null(config);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveHub_When_DestinationAddressUsed()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        var hubEndpoint = transport.DispatchEndpoints.FirstOrDefault(e => e.Destination is EventHubTopic);
        Assert.NotNull(hubEndpoint);

        var destinationAddress = hubEndpoint.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve hub destination address");
        Assert.NotNull(endpoint);
        Assert.IsType<EventHubTopic>(endpoint.Destination);
        Assert.Same(hubEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_UnknownUri()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        var unknownUri = new Uri("http://unknown-host/nonexistent");

        // act
        var found = transport.TryGetDispatchEndpoint(unknownUri, out var endpoint);

        // assert
        Assert.False(found, "TryGetDispatchEndpoint should return false for unknown URI");
        Assert.Null(endpoint);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateHubConfig_When_SendRoute()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var hubEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination is EventHubTopic && e.Kind == DispatchEndpointKind.Default
        );

        // assert
        Assert.NotNull(hubEndpoint);
        Assert.StartsWith("h/", hubEndpoint.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateHubConfig_When_PublishRoute()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var hubEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination is EventHubTopic && e.Kind == DispatchEndpointKind.Default
        );

        // assert
        Assert.NotNull(hubEndpoint);
        Assert.StartsWith("h/", hubEndpoint.Name);
    }

    [Fact]
    public void Schema_Should_BeEventHub_When_TransportCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // assert
        Assert.Equal("eventhub", transport.Schema);
    }

    [Fact]
    public void Describe_Should_ReturnDescription_When_EventHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description);
        Assert.Equal("eventhub", description.Schema);
        Assert.Equal("EventHubMessagingTransport", description.TransportType);
        Assert.NotNull(description.Topology);
    }

    [Fact]
    public void Describe_Should_IncludeHubEntities_When_EventHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);
        Assert.Contains(description.Topology!.Entities, e => e.Kind == "hub");
    }

    [Fact]
    public void Convention_Should_SetErrorAndSkippedEndpoints_When_DefaultEndpoint()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        Assert.NotNull(receiveEndpoint.ErrorEndpoint);
        Assert.NotNull(receiveEndpoint.SkippedEndpoint);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveNullBatchMode_When_NotExplicitlyConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("eventhub:///h/my-hub"));

        // assert
        var ehConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(config);
        Assert.Null(ehConfig.BatchMode);
    }

    [Fact]
    public void DispatchEndpointConfiguration_Should_HaveBatchMode_When_Set()
    {
        // arrange
        var config = new EventHubDispatchEndpointConfiguration
        {
            HubName = "test-hub",
            Name = "test-hub",
            BatchMode = EventHubBatchMode.Batch
        };

        // assert
        Assert.Equal(EventHubBatchMode.Batch, config.BatchMode);
    }

    [Fact]
    public void DispatchEndpointConfiguration_Should_DefaultToNullBatchMode()
    {
        // arrange
        var config = new EventHubDispatchEndpointConfiguration
        {
            HubName = "test-hub",
            Name = "test-hub"
        };

        // assert
        Assert.Null(config.BatchMode);
    }

    [Fact]
    public void ConfigureDefaults_Should_SetDefaultBatchMode_When_Called()
    {
        // arrange
        var config = new EventHubBusDefaults();

        // act
        config.DefaultBatchMode = EventHubBatchMode.Batch;

        // assert
        Assert.Equal(EventHubBatchMode.Batch, config.DefaultBatchMode);
    }

    [Fact]
    public void EventHubBusDefaults_Should_DefaultToSingleBatchMode()
    {
        // arrange & act
        var config = new EventHubBusDefaults();

        // assert
        Assert.Equal(EventHubBatchMode.Single, config.DefaultBatchMode);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeConnectionManager_When_Called()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act & assert - no exception thrown
        await transport.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act - call twice
        await transport.DisposeAsync();
        await transport.DisposeAsync();

        // assert - no exception
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configure,
        Action<IEventHubMessagingTransportDescriptor>? configureTransport = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport?.Invoke(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
