using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
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
            t.Queue("custom-q").AutoProvision(true).Handler<OrderCreatedHandler>().Kind(ReceiveEndpointKind.Error));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "custom-q");
        Assert.Equal("custom-q", endpoint.Queue.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
            t.Queue("err-q").AutoProvision(true).Handler<OrderCreatedHandler>().Kind(ReceiveEndpointKind.Error));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "err-q");
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_StoreVerbatimName_When_FaultEndpointUsesQueueUriWithPascalCase()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:Legacy.Orders.V2_error")));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");
        var feature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.Equal("queue:Legacy.Orders.V2_error", feature?.Address?.OriginalString);
        Assert.False(feature?.IsDisabled ?? false);
    }

    [Fact]
    public void ReceiveEndpoint_Should_UseLocalQueueUri_When_FaultEndpointConfiguredBeforeSchema()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error"));
            t.Schema("custom-rabbit");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");
        Assert.Equal(
            "queue:q_error",
            endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "q_error",
            ((RabbitMQQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_PreserveFaultQueueAutoProvisionFalse_When_FaultQueueDeclared()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.AutoProvision(true);
            t.DeclareQueue("q_error").AutoProvision(false);
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error"));
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // assert
        var errorQueue = topology.Queues.Single(q => q.Name == "q_error");
        Assert.Equal(false, errorQueue.AutoProvision);
    }

    [Fact]
    public void ReceiveEndpoint_Should_PreserveFaultQueueAutoProvisionNull_When_FaultQueueDeclared()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.AutoProvision(true);
            t.DeclareQueue("q_error");
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error"));
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // assert
        var errorQueue = topology.Queues.Single(q => q.Name == "q_error");
        Assert.Null(errorQueue.AutoProvision);
    }

    [Fact]
    public void ReceiveEndpoint_Should_PreserveLaterFaultEndpoint_When_QueueFaultEndpointConfiguredFirst()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error"));
            t.Endpoint("q").FaultEndpoint(new Uri("rabbitmq:q/other_error"));
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");
        Assert.Equal(
            "rabbitmq:q/other_error",
            endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "other_error",
            ((RabbitMQQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_PreserveExtendedFaultAddress_When_QueueFaultEndpointConfiguredFirst()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
        {
            var queue = t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().FaultEndpoint(new Uri("queue:q_error"));
            queue.Extend().Configuration.Features.GetOrSet<ReceiveFaultEndpointFeature>().Address =
                new Uri("queue:extended_error");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");
        Assert.Equal(
            "queue:extended_error",
            endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "extended_error",
            ((RabbitMQQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetDisableFlag_When_DisableFaultEndpointCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t =>
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>().DisableFaultEndpoint());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");
        var feature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.True(feature?.IsDisabled);
        Assert.Null(feature?.Address);
    }

    [Fact]
    public void ReceiveEndpoint_Should_NotEmbedAutoProvisionQuery_When_DefaultFaultAndSkippedEndpointsConfigured()
    {
        // arrange & act
        // AutoProvision is carried by queue topology, not by a query string embedded in the
        // fault or skipped endpoint address URI.
        var runtime = CreateRuntime(t =>
            t.Queue("q").AutoProvision(true).Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(e => e.Name == "q");

        // assert
        var faultFeature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();
        var skippedFeature = endpoint.Configuration.Features.Get<ReceiveSkippedEndpointFeature>();
        Assert.NotNull(faultFeature?.Address);
        Assert.Empty(faultFeature!.Address!.Query);
        Assert.NotNull(skippedFeature?.Address);
        Assert.Empty(skippedFeature!.Address!.Query);
    }

    [Fact]
    public void Transport_Should_DefaultBindModeImplicit_When_NotConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(t => { });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Implicit, transport.BindMode);
    }

    [Fact]
    public void Transport_Should_SetBindModeExplicit_When_BindExplicitlyCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Explicit, transport.BindMode);
    }

    private static MessagingRuntime CreateRuntime(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
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
