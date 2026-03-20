using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

public class RabbitMQHandlerBindingTests
{
    [Fact]
    public void BindHandlersImplicitly_Should_AutoDiscoverHandlers_When_HandlersRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindHandlersImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - implicit binding should auto-create receive endpoints for registered handlers
        Assert.NotEmpty(transport.ReceiveEndpoints);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindHandlersExplicitly_Should_ThrowOnBuild_When_HandlersNotManuallyBound()
    {
        // arrange & act & assert - registering a handler but not manually binding it
        // should throw because the inbound route is unconnected
        Assert.ThrowsAny<InvalidOperationException>(() => CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindHandlersExplicitly()));
    }

    [Fact]
    public void BindHandlersExplicitly_Should_BindManualEndpoints_When_EndpointDeclared()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("q").AutoProvision(true);
                t.Endpoint("ep").Queue("q").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - manually declared endpoint should exist
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == "ep");
    }

    [Fact]
    public void BindHandlersExplicitly_Should_NotAutoCreateEndpoints_When_NoHandlersRegistered()
    {
        // arrange & act - no handlers registered, explicit binding
        var runtime = CreateRuntime(_ => { }, t => t.BindHandlersExplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - no auto-created receive endpoints
        Assert.DoesNotContain(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindHandlersImplicitly_Should_BeDefault_When_NothingConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            { } // no binding mode call - default should be implicit
        );
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - default implicit binding should auto-create endpoints
        Assert.NotEmpty(transport.ReceiveEndpoints);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
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
