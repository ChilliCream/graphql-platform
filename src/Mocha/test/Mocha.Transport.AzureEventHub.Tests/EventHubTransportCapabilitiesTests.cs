using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests;

public class EventHubTransportCapabilitiesTests
{
    [Fact]
    public void Capabilities_Should_BeSendAndPublishSubscribe_When_EventHubTransportRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var capabilities = transport.Capabilities;

        // assert
        const MessagingTransportCapabilities expected =
            MessagingTransportCapabilities.Send | MessagingTransportCapabilities.PublishSubscribe;
        Assert.Equal(expected, capabilities);
    }

    [Fact]
    public void Capabilities_Should_NotIncludeRequestReply_When_EventHubTransportRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var hasRequestReply =
            (transport.Capabilities & MessagingTransportCapabilities.RequestReply)
            == MessagingTransportCapabilities.RequestReply;

        // assert
        Assert.False(hasRequestReply);
    }

    [Fact]
    public void Capabilities_Should_NotIncludeScheduledDelivery_When_EventHubTransportRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var hasScheduledDelivery =
            (transport.Capabilities & MessagingTransportCapabilities.ScheduledDelivery)
            == MessagingTransportCapabilities.ScheduledDelivery;

        // assert
        Assert.False(hasScheduledDelivery);
    }

    [Fact]
    public void BuildRuntime_Should_BindSendRoute_When_OneWayRequestHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var route = runtime.Router.InboundRoutes.Single(r =>
            r.Kind == InboundRouteKind.Send && r.MessageType?.RuntimeType == typeof(ProcessPayment));

        // assert
        Assert.NotNull(route.Endpoint);
        Assert.Same(transport, route.Endpoint!.Transport);
    }

    [Fact]
    public void BuildRuntime_Should_NotUseDefaultConsumerGroup_When_OneWayRequestHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // act
        var endpoint = runtime.Router.InboundRoutes
            .Where(r => r.Kind == InboundRouteKind.Send && r.MessageType?.RuntimeType == typeof(ProcessPayment))
            .Select(r => Assert.IsType<EventHubReceiveEndpoint>(r.Endpoint))
            .Single();

        // assert
        Assert.NotEqual("$Default", endpoint.Configuration.ConsumerGroup);
    }

    [Fact]
    public void BuildRuntime_Should_BindSubscribeRoute_When_EventHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // act
        var route = runtime.Router.InboundRoutes.Single(r =>
            r.Kind == InboundRouteKind.Subscribe && r.MessageType?.RuntimeType == typeof(OrderCreated));

        // assert
        Assert.NotNull(route.Endpoint);
        Assert.Same(transport, route.Endpoint!.Transport);
    }

    [Fact]
    public void DiscoverEndpoints_Should_NotCreateReplyEndpoints_When_EventHubTransport()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // act
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // assert
        Assert.Null(transport.ReplyReceiveEndpoint);
        Assert.Null(transport.ReplyDispatchEndpoint);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_ExplicitReceiveEndpointKindIsReply()
    {
        // arrange
        var build = () => CreateRuntimeWithExplicitTransport(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("explicit-reply")
                    .Handler<OrderCreatedHandler>()
                    .Hub("custom-hub")
                    .Kind(ReceiveEndpointKind.Reply);
            });

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("does not support receive endpoint", ex.Message);
        Assert.Contains("RequestReply", ex.Message);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_ExplicitDispatchEndpointKindIsReply()
    {
        // arrange
        var build = () => CreateRuntimeWithExplicitTransport(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("recv-ep").Handler<OrderCreatedHandler>().Hub("custom-hub");
                var dispatch = t.DispatchEndpoint("explicit-dispatch-reply").ToHub("custom-hub");
                dispatch.Extend().Configuration.Kind = DispatchEndpointKind.Reply;
            });

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("does not support dispatch endpoint", ex.Message);
        Assert.Contains("RequestReply", ex.Message);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnNull_When_EventHubRepliesPath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var configuration = transport.CreateEndpointConfiguration(context, new Uri("eventhub:///replies"));

        // assert
        Assert.Null(configuration);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnHubConfig_When_HubNamedRepliesViaSchemeRelativePath()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var configuration = transport.CreateEndpointConfiguration(context, new Uri("eventhub:///h/replies"));

        // assert
        var hubConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(configuration);
        Assert.Equal("replies", hubConfig.HubName);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnHubConfig_When_HubNamedRepliesViaShorthand()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var configuration = transport.CreateEndpointConfiguration(context, new Uri("hub://replies"));

        // assert
        var hubConfig = Assert.IsType<EventHubDispatchEndpointConfiguration>(configuration);
        Assert.Equal("replies", hubConfig.HubName);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_ExplicitCommandUsesDefaultConsumerGroup()
    {
        // arrange
        var build = () => CreateRuntimeWithExplicitTransport(
            b => b.AddRequestHandler<ProcessPaymentHandler>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("payments")
                    .Hub("payments")
                    .ConsumerGroup("$Default")
                    .Handler<ProcessPaymentHandler>();
            });

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("requires exactly one logical owner consumer group", ex.Message);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_ExplicitDispatchEndpointSharesSendAndPublishHub()
    {
        // arrange
        var build = () => CreateRuntimeWithExplicitTransport(
            b => b.AddMessage<ProcessPayment>(_ => { }),
            t =>
            {
                t.DispatchEndpoint("shared")
                    .ToHub("shared")
                    .Send<ProcessPayment>()
                    .Publish<ProcessPayment>();
            });

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("cannot bind send and publish routes", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_Should_ThrowBeforeDispatch_When_ScheduledDeliveryRequested()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddEventHandler<OrderCreatedHandler>());
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new PublishOptions
        {
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await bus.PublishAsync(
                new OrderCreated { OrderId = "ORD-SCHEDULED" },
                options,
                CancellationToken.None));

        // assert
        Assert.Contains("Scheduled delivery is not supported", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Should_ThrowBeforeDispatch_When_ScheduledDeliveryRequested()
    {
        // arrange
        await using var provider = CreateProvider(b => b.AddRequestHandler<ProcessPaymentHandler>());
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new SendOptions
        {
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        // act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await bus.SendAsync(
                new ProcessPayment { OrderId = "PAY-SCHEDULED", Amount = 42m },
                options,
                CancellationToken.None));

        // assert
        Assert.Contains("Scheduled delivery is not supported", ex.Message);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_RequestResponseHandlerRegisteredAndOnlyEventHubAvailable()
    {
        // arrange
        var build = () => CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("No configured transport supports", ex.Message);
        Assert.Contains("RequestReply", ex.Message);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var provider = CreateProvider(configure);
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    private static ServiceProvider CreateProvider(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddEventHub(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .Services
            .BuildServiceProvider();
    }

    private static MessagingRuntime CreateRuntimeWithExplicitTransport(
        Action<IMessageBusHostBuilder> configure,
        Action<IEventHubMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
    }
}
