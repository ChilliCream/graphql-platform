using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class RoutingKeyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_negativeTimeout = TimeSpan.FromSeconds(2);
    private readonly RabbitMQFixture _fixture;

    public RoutingKeyTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToMatchingQueue_When_RoutingKeyMatchesBindingPattern()
    {
        // arrange
        var usRecorder = new MessageRecorder();
        var euRecorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("us", usRecorder)
            .AddKeyedSingleton("eu", euRecorder)
            .AddMessageBus()
            .AddConsumer<UsRegionConsumer>()
            .AddConsumer<EuRegionConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();

                t.DeclareExchange("region-topic").Type(RabbitMQExchangeType.Topic);
                t.DeclareQueue("us-queue");
                t.DeclareQueue("eu-queue");
                t.DeclareBinding("region-topic", "us-queue").RoutingKey("us.*");
                t.DeclareBinding("region-topic", "eu-queue").RoutingKey("eu.*");

                t.Queue("us-queue").AutoBind(false).Consumer<UsRegionConsumer>();
                t.Queue("eu-queue").AutoBind(false).Consumer<EuRegionConsumer>();
                t.DispatchEndpoint("region-dispatch").ToExchange("region-topic").Publish<RegionEvent>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.east", Payload = "hello-us" },
            CancellationToken.None);

        // assert
        Assert.True(await usRecorder.WaitAsync(s_timeout), "US handler did not receive the event");
        var usMessage = Assert.Single(usRecorder.Messages);
        var usEvent = Assert.IsType<RegionEvent>(usMessage);
        Assert.Equal("us.east", usEvent.Region);
        Assert.Equal("hello-us", usEvent.Payload);

        Assert.False(
            await euRecorder.WaitAsync(s_negativeTimeout),
            "EU handler should not have received a message routed to us.*");
        Assert.Empty(euRecorder.Messages);
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToAllMatchingQueues_When_WildcardBindingMatches()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<CatchAllConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();

                t.DeclareExchange("wildcard-topic").Type(RabbitMQExchangeType.Topic);
                t.DeclareQueue("catch-all-queue");
                t.DeclareBinding("wildcard-topic", "catch-all-queue").RoutingKey("#");

                t.Queue("catch-all-queue").AutoBind(false).Consumer<CatchAllConsumer>();
                t.DispatchEndpoint("wildcard-dispatch").ToExchange("wildcard-topic").Publish<RegionEvent>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.east", Payload = "msg-1" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "eu.west", Payload = "msg-2" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "ap.southeast.sg", Payload = "msg-3" },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 3),
            "Catch-all handler did not receive all 3 events within timeout");

        Assert.Equal(3, recorder.Messages.Count);

        var payloads = recorder
            .Messages.Cast<RegionEvent>()
            .Select(e => e.Payload)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["msg-1", "msg-2", "msg-3"], payloads);
    }

    [Fact]
    public async Task PublishAsync_Should_NotDeliverToQueue_When_RoutingKeyDoesNotMatchBinding()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<CatchAllConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();

                t.DeclareExchange("nomatch-topic").Type(RabbitMQExchangeType.Topic);
                t.DeclareQueue("nomatch-queue");
                t.DeclareBinding("nomatch-topic", "nomatch-queue").RoutingKey("eu.*");

                t.Queue("nomatch-queue").AutoBind(false).Consumer<CatchAllConsumer>();
                t.DispatchEndpoint("nomatch-dispatch").ToExchange("nomatch-topic").Publish<RegionEvent>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish with a routing key that does not match "eu.*"
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.east", Payload = "should-not-arrive" },
            CancellationToken.None);

        // assert
        Assert.False(
            await recorder.WaitAsync(s_negativeTimeout),
            "Handler should not have received a message when the routing key does not match the binding");
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task PublishAsync_Should_SetRoutingKeyHeader_When_ExtractorConfigured()
    {
        // arrange
        var tracker = new RoutingKeyTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<CatchAllConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();

                t.DeclareExchange("header-topic").Type(RabbitMQExchangeType.Topic);
                t.DeclareQueue("header-queue");
                t.DeclareBinding("header-topic", "header-queue").RoutingKey("#");

                t.Queue("header-queue").AutoBind(false).Consumer<CatchAllConsumer>();
                t.DispatchEndpoint("header-dispatch")
                    .ToExchange("header-topic")
                    .Publish<RegionEvent>()
                    .UseDispatch(
                        new DispatchMiddlewareConfiguration(
                            (_, next) =>
                                context =>
                                {
                                    if (context.Headers.TryGet(RabbitMQMessageHeaders.RoutingKey, out var routingKey))
                                    {
                                        tracker.CapturedKeys.Add(routingKey);
                                    }

                                    return next(context);
                                },
                            "routing-key-spy"),
                        after: "RabbitMQRoutingKey");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new RegionEvent { Region = "ap.southeast", Payload = "header-check" },
            CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");

        var capturedKey = Assert.Single(tracker.CapturedKeys);
        Assert.Equal("ap.southeast", capturedKey);
    }

    [Fact]
    public async Task PublishAsync_Should_RouteAllBoundKeys_When_MultipleBindingsDeclaredForSamePair()
    {
        // arrange
        // each routing key is attached via its own binding declaration between the exchange and queue.
        var usRecorder = new MessageRecorder();
        var euRecorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("us", usRecorder)
            .AddKeyedSingleton("eu", euRecorder)
            .AddMessageBus()
            .AddConsumer<UsRegionConsumer>()
            .AddConsumer<EuRegionConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();

                t.DeclareExchange("region-direct").Type(RabbitMQExchangeType.Direct);
                t.DeclareQueue("us-queue");
                t.DeclareQueue("eu-queue");

                t.DeclareBinding("region-direct", "us-queue").RoutingKey("us.east");
                t.DeclareBinding("region-direct", "us-queue").RoutingKey("us.west");
                t.DeclareBinding("region-direct", "eu-queue").RoutingKey("eu.north");
                t.DeclareBinding("region-direct", "eu-queue").RoutingKey("eu.south");

                t.Queue("us-queue").AutoBind(false).Consumer<UsRegionConsumer>();
                t.Queue("eu-queue").AutoBind(false).Consumer<EuRegionConsumer>();
                t.DispatchEndpoint("region-dispatch").ToExchange("region-direct").Publish<RegionEvent>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.east", Payload = "us-east" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.west", Payload = "us-west" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "eu.north", Payload = "eu-north" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "eu.south", Payload = "eu-south" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new RegionEvent { Region = "ap.southeast", Payload = "unmatched" },
            CancellationToken.None);

        // assert
        Assert.True(
            await usRecorder.WaitAsync(s_timeout, expectedCount: 2),
            "Both bindings to the us queue should deliver their routing keys");
        Assert.True(
            await euRecorder.WaitAsync(s_timeout, expectedCount: 2),
            "Both bindings to the eu queue should deliver their routing keys");

        var usPayloads = usRecorder
            .Messages.Cast<RegionEvent>()
            .Select(e => e.Payload)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        var euPayloads = euRecorder
            .Messages.Cast<RegionEvent>()
            .Select(e => e.Payload)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["us-east", "us-west"], usPayloads);
        Assert.Equal(["eu-north", "eu-south"], euPayloads);
    }

    public sealed class RegionEvent
    {
        public required string Region { get; init; }
        public required string Payload { get; init; }
    }

    public sealed class UsRegionConsumer([FromKeyedServices("us")] MessageRecorder recorder) : IConsumer<RegionEvent>
    {
        public ValueTask ConsumeAsync(IConsumeContext<RegionEvent> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }

    public sealed class EuRegionConsumer([FromKeyedServices("eu")] MessageRecorder recorder) : IConsumer<RegionEvent>
    {
        public ValueTask ConsumeAsync(IConsumeContext<RegionEvent> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }

    public sealed class CatchAllConsumer(MessageRecorder recorder) : IConsumer<RegionEvent>
    {
        public ValueTask ConsumeAsync(IConsumeContext<RegionEvent> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }

    public sealed class RoutingKeyTracker
    {
        public ConcurrentBag<string> CapturedKeys { get; } = [];
    }
}
