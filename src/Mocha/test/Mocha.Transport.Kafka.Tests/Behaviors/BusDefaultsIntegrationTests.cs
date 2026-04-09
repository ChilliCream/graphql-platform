using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Behaviors;

[Collection("Kafka")]
public class BusDefaultsIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly KafkaFixture _fixture;

    public BusDefaultsIntegrationTests(KafkaFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_DefaultPartitionsConfigured()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddKafka(t =>
            {
                t.BootstrapServers(ctx.BootstrapServers);
                t.ConfigureDefaults(d => d.Topic.Partitions = 3);
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
    public async Task ConfigureDefaults_Should_DeliverMessages_When_TopicConfigsApplied()
    {
        // arrange
        var recorder = new MessageRecorder();
        var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddKafka(t =>
            {
                t.BootstrapServers(ctx.BootstrapServers);
                t.ConfigureDefaults(d =>
                {
                    d.Topic.Partitions = 1;
                    d.Topic.TopicConfigs = new Dictionary<string, string>
                    {
                        ["retention.ms"] = "86400000"
                    };
                });
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-CONFIGS" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-CONFIGS", order.OrderId);
    }
}
