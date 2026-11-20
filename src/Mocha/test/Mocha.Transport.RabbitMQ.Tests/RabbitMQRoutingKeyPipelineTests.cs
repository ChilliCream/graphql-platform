using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Middlewares;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQRoutingKeyPipelineTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Serialization")]
    public async Task ExecuteAsync_Should_ExtractRoutingKeyFromNewMessage_When_BusMiddlewareReplacesMessage(
        string? before)
    {
        // arrange
        var replace = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                context.Message = new RegionEvent { Region = "eu.west" };
                return next(context);
            },
            "ReplaceMessage");

        // act
        var routingKey = await DispatchAsync(
            new RegionEvent { Region = "us.east" },
            bus => bus.UseDispatch(replace, before: before),
            static _ => { });

        // assert
        Assert.Equal("eu.west", routingKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Serialization")]
    public async Task ExecuteAsync_Should_ExtractRoutingKeyFromChangedMessage_When_BusMiddlewareMutatesMessage(
        string? before)
    {
        // arrange
        var mutate = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                ((RegionEvent)context.Message!).Region = "eu.west";
                return next(context);
            },
            "MutateMessage");

        // act
        var routingKey = await DispatchAsync(
            new RegionEvent { Region = "us.east" },
            bus => bus.UseDispatch(mutate, before: before),
            static _ => { });

        // assert
        Assert.Equal("eu.west", routingKey);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ExposeExtractedRoutingKey_When_BusMiddlewareRunsAfterSerialization()
    {
        // arrange
        string? observed = null;
        var observe = new DispatchMiddlewareConfiguration(
            (_, next) => context =>
            {
                observed = context.Headers.Get(RabbitMQMessageHeaders.RoutingKey);
                return next(context);
            },
            "ObserveRoutingKey");

        // act
        await DispatchAsync(
            new RegionEvent { Region = "us.east" },
            bus => bus.UseDispatch(observe, after: DispatchMiddlewares.Serialization.Key),
            static _ => { });

        // assert
        Assert.Equal("us.east", observed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ExposeExtractedRoutingKey_When_TransportMiddlewareRunsAfterExtractor()
    {
        // arrange
        string? observed = null;
        var observe = new DispatchMiddlewareConfiguration(
            (_, next) => context =>
            {
                observed = context.Headers.Get(RabbitMQMessageHeaders.RoutingKey);
                return next(context);
            },
            "ObserveRoutingKey");

        // act
        await DispatchAsync(
            new RegionEvent { Region = "us.east" },
            static _ => { },
            transport => transport.UseDispatch(observe, after: RabbitMQDispatchMiddlewares.RoutingKey.Key));

        // assert
        Assert.Equal("us.east", observed);
    }

    private static async Task<string?> DispatchAsync(
        RegionEvent message,
        Action<IMessageBusBuilder> configureBus,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        string? routingKey = null;
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<RegionEvent>(m =>
        {
            m.UseRabbitMQRoutingKey<RegionEvent>(e => e.Region);
            m.Publish(route => route.ToRabbitMQExchange("regions"));
        });
        builder.ConfigureMessageBus(bus =>
        {
            bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, _) => context =>
                    {
                        routingKey = context.Envelope!.Headers!.Get(RabbitMQMessageHeaders.RoutingKey);
                        return default;
                    },
                    "CaptureRoutingKey"),
                after: DispatchMiddlewares.Serialization.Key);
            configureBus(bus);
        });
        await using var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        var messageType = runtime.GetMessageType(typeof(RegionEvent));
        var endpoint = runtime.GetPublishEndpoint(messageType);
        var context = new DispatchContext { Message = message };
        context.Initialize(runtime.Services, endpoint, runtime, messageType, CancellationToken.None);

        try
        {
            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            context.Reset();
        }

        return routingKey;
    }

    public sealed class RegionEvent
    {
        public required string Region { get; set; }
    }
}
