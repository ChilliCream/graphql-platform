using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Messaging)]
public class ConcurrencyTests
{
    private readonly RabbitMQFixture _fixture;

    public ConcurrencyTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<SlowOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(1).MaxPrefetch(20))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(TestTimeouts.Signal, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.Equal(1, tracker.PeakConcurrency);
    }

    [Fact]
    public async Task Handler_Should_AllowParallelism_When_MaxConcurrencyGreaterThanOne()
    {
        // arrange
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        const int messageCount = 20;

        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<SlowOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("slow-ep").Handler<SlowOrderHandler>().MaxConcurrency(5).MaxPrefetch(20))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(TestTimeouts.Signal, expectedCount: messageCount),
            $"Handler did not process all {messageCount} messages within timeout");

        Assert.True(tracker.PeakConcurrency > 1, $"Expected parallelism > 1, but peak was {tracker.PeakConcurrency}");
        Assert.True(tracker.PeakConcurrency <= 5, $"Expected peak concurrency <= 5, but was {tracker.PeakConcurrency}");
    }

    [Fact]
    public async Task Handler_Should_LimitConcurrency_When_BacklogIsProcessedAfterRecovery()
    {
        // arrange
        var timeout = TestTimeouts.Signal;
        const int messageCount = 10;
        var tracker = new ConcurrencyTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        ((ConnectionFactory)vhost.ConnectionFactory).NetworkRecoveryInterval = TimeSpan.FromSeconds(2);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<SlowOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("recovery-orders")
                .Handler<SlowOrderHandler>()
                .MaxConcurrency(1)
                .MaxPrefetch(1))
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "before" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(timeout));

        // act
        await using (await vhost.Faults.DenyDeclaresAsync())
        {
            await vhost.Faults.CloseConsumerConnectionAsync("recovery-orders");
            await vhost.Broker.WaitForNoConsumerAsync("recovery-orders");
            for (var i = 0; i < messageCount; i++)
            {
                await messageBus.PublishAsync(
                    new OrderCreated { OrderId = $"ORD-{i}" },
                    TestContext.Current.CancellationToken);
            }
        }

        // assert
        Assert.True(await recorder.WaitAsync(timeout, expectedCount: messageCount));
        Assert.Equal(1, tracker.PeakConcurrency);
    }

    public sealed class SlowOrderHandler(ConcurrencyTracker tracker, MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(500, cancellationToken);
            }
            finally
            {
                tracker.Exit();
                recorder.Record(message);
            }
        }
    }
}
