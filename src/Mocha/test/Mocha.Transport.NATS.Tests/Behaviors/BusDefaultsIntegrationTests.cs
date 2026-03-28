using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class BusDefaultsIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly NatsFixture _fixture;

    public BusDefaultsIntegrationTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_CustomDefaultsApplied()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(t =>
            {
                t.ConfigureDefaults(d =>
                {
                    d.Stream.MaxAge = TimeSpan.FromHours(1);
                    d.Consumer.MaxAckPending = 50;
                });
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULTS" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-DEFAULTS", order.OrderId);
    }

    [Fact]
    public async Task AddNats_Should_DeliverMessages_When_DefaultConfiguration()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULT" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-DEFAULT", order.OrderId);
    }
}
