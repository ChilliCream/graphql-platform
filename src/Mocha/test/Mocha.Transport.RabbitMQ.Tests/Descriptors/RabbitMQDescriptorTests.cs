using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

public class RabbitMQDescriptorTests
{
    [Fact]
    public void Transport_Should_UseCustomSchema_When_SchemaConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.Schema("custom"));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.Equal("custom", transport.Schema);
    }

    [Fact]
    public void Transport_Should_UseCustomName_When_NameConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.Name("my-transport"));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.Equal("my-transport", transport.Name);
    }

    [Fact]
    public void Transport_Should_BeDefault_When_IsDefaultTransportCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.IsDefaultTransport());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - with a single transport, verify it was built successfully and is the only one
        Assert.NotNull(transport);
        Assert.Single(runtime.Transports);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetQueue_When_ToQueueCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<RabbitMQQueue>(endpoint.Destination);
        Assert.Equal("my-q", ((RabbitMQQueue)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Queue);
        Assert.Equal("my-q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetExchange_When_ToExchangeCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("my-ex");
            t.DispatchEndpoint("ep").ToExchange("my-ex");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
        Assert.Equal("my-ex", ((RabbitMQExchange)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Exchange);
        Assert.Equal("my-ex", endpoint.Exchange!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterSendRoute_When_SendCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("q").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<RabbitMQQueue>(endpoint.Destination);
        Assert.Equal("q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterPublishRoute_When_PublishCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("ex");
            t.DispatchEndpoint("ep").ToExchange("ex").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
        Assert.Equal("ex", endpoint.Exchange!.Name);
    }

    // --- Receive Endpoint Descriptor Tests ---

    [Fact]
    public void ReceiveEndpoint_Should_SetQueueName_When_QueueCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("custom-q").AutoProvision(true);
            t.Endpoint("ep").Queue("custom-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.Equal("custom-q", endpoint.Queue.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("err-q").AutoProvision(true);
            t.Endpoint("ep").Queue("err-q").Kind(ReceiveEndpointKind.Error);
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_StoreVerbatimName_When_ErrorQueueNamedWithPascalCase()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q").AutoProvision(true);
            t.Endpoint("ep").Queue("q").ErrorQueue("Legacy.Orders.V2_error");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.Equal("Legacy.Orders.V2_error", endpoint.Configuration.ErrorQueue.QueueName);
        Assert.False(endpoint.Configuration.ErrorQueue.IsDisabled);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetDisableFlag_When_DisableErrorQueueCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q").AutoProvision(true);
            t.Endpoint("ep").Queue("q").DisableErrorQueue();
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.True(endpoint.Configuration.ErrorQueue.IsDisabled);
    }

    [Fact]
    public void ReceiveEndpoint_Should_RejectAutoProvisionUriSuffix_When_SatelliteConfigured()
    {
        // arrange & act
        // Satellite AutoProvision is carried by the typed RabbitMQSatelliteConfiguration, not by
        // a ?autoProvision= query string embedded in the satellite queue address URI (D13).
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q").AutoProvision(true);
            t.Endpoint("ep").Queue("q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "ep");

        // assert
        // The satellite endpoint URIs must not embed a ?autoProvision= query parameter; the typed
        // configuration field is the sole carrier of the provisioning decision.
        Assert.NotNull(endpoint.Configuration.ErrorEndpoint);
        Assert.Empty(endpoint.Configuration.ErrorEndpoint!.Query);
        Assert.NotNull(endpoint.Configuration.SkippedEndpoint);
        Assert.Empty(endpoint.Configuration.SkippedEndpoint!.Query);
    }

    [Fact]
    public void Transport_Should_DefaultAutoBindTrue_When_NotConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(t => { });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.True(transport.AutoBind);
    }

    [Fact]
    public void Transport_Should_SetAutoBindFalse_When_AutoBindFalseCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.AutoBind(false));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.False(transport.AutoBind);
    }

    private static MessagingRuntime CreateRuntime(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
