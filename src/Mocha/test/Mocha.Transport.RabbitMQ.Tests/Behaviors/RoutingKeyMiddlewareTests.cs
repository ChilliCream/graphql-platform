using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public class RoutingKeyMiddlewareTests
{
    private static readonly TimeSpan s_negativeTimeout = TimeSpan.FromSeconds(2);
    private readonly RabbitMQFixture _fixture;

    public RoutingKeyMiddlewareTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteByNewMessage_When_BusMiddlewareReplacesMessageBeforeSerialization()
    {
        // arrange
        var usRecorder = new MessageRecorder();
        var euRecorder = new MessageRecorder();
        var retag = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                var original = (RegionEvent)context.Message!;
                context.Message = new RegionEvent { Region = "eu.west", Payload = original.Payload + "-retagged" };
                return next(context);
            },
            "Retag");
        await using var vhost = await _fixture.CreateVhostAsync();
        var services = new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("us", usRecorder)
            .AddKeyedSingleton("eu", euRecorder);
        var builder = services.AddMessageBus();
        builder.ConfigureMessageBus(bus => bus.UseDispatch(retag, before: DispatchMiddlewares.Serialization.Key));
        await using var bus = await builder
            .AddConsumer<UsRegionConsumer>()
            .AddConsumer<EuRegionConsumer>()
            .AddMessage<RegionEvent>(m => m.UseRabbitMQRoutingKey<RegionEvent>(msg => msg.Region))
            .AddRabbitMQ(t =>
            {
                t.BindExplicitly();

                t.DeclareExchange("region-topic").Type(RabbitMQExchangeType.Topic);
                t.DeclareQueue("us-queue");
                t.DeclareQueue("eu-queue");
                t.DeclareBinding("region-topic", "us-queue").RoutingKey("us.*");
                t.DeclareBinding("region-topic", "eu-queue").RoutingKey("eu.*");

                t.Queue("us-queue").BindExplicitly().Consumer<UsRegionConsumer>();
                t.Queue("eu-queue").BindExplicitly().Consumer<EuRegionConsumer>();
                t.DispatchEndpoint("region-dispatch").ToExchange("region-topic").Publish<RegionEvent>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new RegionEvent { Region = "us.east", Payload = "p" },
            CancellationToken.None);

        // assert
        Assert.True(await euRecorder.WaitAsync(TestTimeouts.Signal), "EU handler did not receive the retagged event");
        var euEvent = Assert.IsType<RegionEvent>(Assert.Single(euRecorder.Messages));
        Assert.Equal("eu.west/p-retagged", $"{euEvent.Region}/{euEvent.Payload}");
        Assert.False(
            await usRecorder.WaitAsync(s_negativeTimeout),
            "US handler should not have received an event retagged to eu.west");
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
}
