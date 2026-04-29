using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresHandlerClaimTests
{
    [Fact]
    public void Handler_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - convention name for OrderCreatedHandler is "order-created"
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");

        Assert.NotNull(endpoint);
        Assert.Equal(ReceiveEndpointKind.Default, endpoint!.Kind);
    }

    [Fact]
    public void Handler_Should_ApplyConfig_When_ConfigureEndpointCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.Handler<OrderCreatedHandler>()
                .ConfigureEndpoint(e => e.Queue("custom-handler-queue")));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - the endpoint should exist with the custom queue name
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");

        Assert.NotNull(endpoint);
        Assert.Equal("custom-handler-queue", endpoint!.Queue.Name);
    }

    [Fact]
    public void Consumer_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.Consumer<OrderSpyConsumer>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - convention name for OrderSpyConsumer is "order-spy"
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-spy");

        Assert.NotNull(endpoint);
        Assert.Equal(ReceiveEndpointKind.Default, endpoint!.Kind);
    }

    [Fact]
    public void Handler_Should_MergeWithExisting_When_ConventionNameMatchesExplicitEndpoint()
    {
        // arrange & act - "order-created" is the convention name for OrderCreatedHandler;
        // creating an explicit endpoint with the same name first should merge.
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.Endpoint("order-created").Queue("merged-queue");
                t.Handler<OrderCreatedHandler>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - should be exactly one endpoint with that name, not two
        var endpoints = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Where(e => e.Name == "order-created")
            .ToList();

        Assert.Single(endpoints);
        Assert.Equal("merged-queue", endpoints[0].Queue.Name);
    }

    [Fact]
    public void Handler_Should_CreateSeparateEndpoints_When_MultipleHandlersClaimed()
    {
        // arrange & act
        var runtime = CreateRuntime(
            b =>
            {
                b.AddEventHandler<OrderCreatedHandler>();
                b.AddEventHandler<PaymentReceivedHandler>();
            },
            t =>
            {
                t.Handler<OrderCreatedHandler>();
                t.Handler<PaymentReceivedHandler>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert - two distinct endpoints for the two handlers
        var orderEndpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");
        var paymentEndpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "payment-received");

        Assert.NotNull(orderEndpoint);
        Assert.NotNull(paymentEndpoint);
        Assert.NotSame(orderEndpoint, paymentEndpoint);
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

    public sealed class PaymentReceived
    {
        public required string PaymentId { get; init; }
    }

    public sealed class PaymentReceivedHandler : IEventHandler<PaymentReceived>
    {
        public ValueTask HandleAsync(PaymentReceived message, CancellationToken cancellationToken) => default;
    }
}
