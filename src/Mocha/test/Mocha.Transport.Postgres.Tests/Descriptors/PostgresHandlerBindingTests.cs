using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

public class PostgresHandlerBindingTests
{
    [Fact]
    public void BindImplicitly_Should_AutoDiscoverHandlers_When_HandlersRegistered()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindImplicitly());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - implicit binding should auto-create receive endpoints for registered handlers
        Assert.NotEmpty(transport.ReceiveEndpoints);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    [Fact]
    public void BindExplicitly_Should_ThrowOnBuild_When_HandlersNotManuallyBound()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>(), t => t.BindExplicitly()));

        Assert.Contains("unbound inbound route", exception.Message);
        Assert.Contains(nameof(OrderCreatedHandler), exception.Message);
        Assert.Contains(InboundRouteKind.Subscribe.ToString(), exception.Message);
        Assert.Contains("explicit bind mode", exception.Message);
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
                t.Queue("q").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - manually declared endpoint should exist
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == "q");
    }

    [Fact]
    public void BindExplicitly_Should_NotAutoCreateEndpoints_When_NoHandlersRegistered()
    {
        // arrange & act - no handlers registered, explicit binding
        var runtime = CreateRuntime(_ => { }, t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

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
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - default implicit binding should auto-create endpoints
        Assert.NotEmpty(transport.ReceiveEndpoints);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Default);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
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
