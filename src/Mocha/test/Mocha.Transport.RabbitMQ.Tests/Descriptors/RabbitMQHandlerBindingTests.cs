using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

public class RabbitMQHandlerBindingTests
{
    [Fact]
    public void BindImplicitly_Should_AutoDiscoverHandlers_When_HandlersRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - implicit binding should auto-create receive endpoints for registered handlers
        Assert.NotEmpty(transport.ReceiveEndpoints);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindExplicitly_Should_ThrowOnBuild_When_HandlersNotManuallyBound()
    {
        // arrange & act & assert - registering a handler but not manually binding it
        // should throw because the inbound route is unconnected
        Assert.ThrowsAny<InvalidOperationException>(() => CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindExplicitly()));
    }

    [Fact]
    public void BindExplicitly_Should_BindManualEndpoints_When_EndpointDeclared()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("q").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - manually declared endpoint should exist
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == "q");
    }

    [Fact]
    public void BindExplicitly_Should_NotAutoCreateEndpoints_When_NoHandlersRegistered()
    {
        // arrange & act - no handlers registered, explicit binding
        var runtime = CreateRuntime(_ => { }, t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // assert - no auto-created receive endpoints
        Assert.DoesNotContain(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindImplicitly_Should_BeDefault_When_NothingConfigured()
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

    [Fact]
    public void BindImplicitly_Should_DescribeConventionTopology_When_HandlersRegistered()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert: the convention exchange chain and the binding into the auto-named queue appear
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void BindExplicitly_Should_SuppressConventionBinds_When_TransportExplicit()
    {
        // arrange
        // BindExplicitly suppresses both discovery and default convention binds.
        // Type-owned exchanges are still created but no exchange-to-queue binds are generated.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void BindExplicitly_Should_DescribeViaUnifiedQueue_When_HandlerAttachedToQueue()
    {
        // arrange
        // A handler placed on a unified Queue() front door constitutes an explicit claim.
        // With BindExplicitly, no convention exchange-to-queue binds are generated.
        // Type-owned exchanges still appear. The endpoint exists under the declared queue name.
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("my-orders").Handler<OrderCreatedHandler>();
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
