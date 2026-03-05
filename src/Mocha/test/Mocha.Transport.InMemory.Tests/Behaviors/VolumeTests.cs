using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class VolumeTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_DeliverAll_When_1000EventsPublished()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        const int eventCount = 1000;

        // act
        for (var i = 1; i <= eventCount; i++)
        {
            await bus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: eventCount),
            $"Handler did not receive all {eventCount} events within timeout");

        Assert.Equal(eventCount, recorder.Messages.Count);
    }

    [Fact]
    public async Task PublishAsync_Should_NotLoseMessages_When_ConcurrentPublishers()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        const int publishersCount = 10;
        const int messagesPerPublisher = 100;
        const int totalMessages = publishersCount * messagesPerPublisher;

        // act - spawn multiple concurrent publishers
        var tasks = Enumerable
            .Range(1, publishersCount)
            .Select(async publisherId =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                for (var i = 1; i <= messagesPerPublisher; i++)
                {
                    await bus.PublishAsync(
                        new OrderCreated { OrderId = $"P{publisherId}-ORD-{i}" },
                        CancellationToken.None);
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: totalMessages),
            $"Handler did not receive all {totalMessages} messages within timeout");

        Assert.Equal(totalMessages, recorder.Messages.Count);
    }
}
