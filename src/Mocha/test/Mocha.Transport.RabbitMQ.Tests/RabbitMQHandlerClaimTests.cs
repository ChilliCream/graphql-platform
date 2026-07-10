using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQHandlerClaimTests
{
    [Fact]
    public void Handler_Should_CreateEndpoint_When_Called()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.Handler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Name == "order-created");

        // assert
        Assert.Contains(typeof(OrderCreatedHandler), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_ApplyConfig_When_ConfigureEndpointCalled()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t => t.Handler<OrderCreatedHandler>()
                .ConfigureEndpoint(e => e.MaxPrefetch(50).MaxConcurrency(5)));
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Name == "order-created");

        // assert
        Assert.Equal(50, endpoint.Configuration.MaxPrefetch);
        Assert.Equal(5, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Consumer_Should_CreateEndpoint_When_Called()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.Consumer<OrderSpyConsumer>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().First(e => e.Name == "order-spy");

        // assert
        Assert.Contains(typeof(OrderSpyConsumer), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_MergeWithExisting_When_ConventionNameMatchesExplicitEndpoint()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.Endpoint("order-created").MaxConcurrency(10);
                t.Handler<OrderCreatedHandler>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoints = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .Where(e => e.Name == "order-created")
            .ToList();

        // assert
        Assert.Single(endpoints);
        Assert.Contains(typeof(OrderCreatedHandler), endpoints[0].Configuration.ConsumerIdentities);
        Assert.Equal(10, endpoints[0].Configuration.MaxConcurrency);
    }

    [Fact]
    public void Handler_Should_CreateSeparateEndpoints_When_MultipleHandlersClaimed()
    {
        // arrange
        var runtime = CreateRuntime(
            b =>
            {
                b.AddEventHandler<OrderCreatedHandler>();
                b.AddRequestHandler<GetOrderStatusHandler>();
            },
            t =>
            {
                t.Handler<OrderCreatedHandler>();
                t.Handler<GetOrderStatusHandler>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var orderEndpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .FirstOrDefault(e => e.Name == "order-created");
        var statusEndpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .FirstOrDefault(e => e.Name == "get-order-status");

        // assert
        Assert.NotNull(orderEndpoint);
        Assert.NotNull(statusEndpoint);
        Assert.NotEqual(orderEndpoint, statusEndpoint);
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
