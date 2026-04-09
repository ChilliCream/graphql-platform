using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Features;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class PartitionRoutingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public PartitionRoutingTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToSamePartition_When_SamePartitionKeyUsed()
    {
        // arrange
        var capture = new PartitionCapture();
        var hubName = _fixture.GetHubForTest("partition");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        const int messageCount = 10;
        const string partitionKey = "tenant-acme";

        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<PartitionSpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send multiple messages with the same partition key
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(
                new OrderCreated { OrderId = $"ORD-{i}" },
                new PublishOptions
                {
                    Headers = new() { ["x-partition-key"] = partitionKey }
                },
                CancellationToken.None);
        }

        // assert - all messages should arrive and be from the same partition
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Did not receive all {messageCount} messages within timeout");

        var partitionIds = capture.PartitionIds.ToArray();
        Assert.All(partitionIds, id => Assert.Equal(partitionIds[0], id));
    }

    [Fact]
    public async Task PublishAsync_Should_DistributeAcrossPartitions_When_DifferentPartitionKeysUsed()
    {
        // arrange
        var capture = new PartitionCapture();
        var hubName = _fixture.GetHubForTest("partition");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        const int messageCount = 20;

        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<PartitionSpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send messages with different partition keys to encourage distribution
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(
                new OrderCreated { OrderId = $"ORD-{i}" },
                new PublishOptions
                {
                    Headers = new() { ["x-partition-key"] = $"tenant-{i}" }
                },
                CancellationToken.None);
        }

        // assert - at least some messages should arrive on different partitions
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Did not receive all {messageCount} messages within timeout");

        var distinctPartitions = capture.PartitionIds.Distinct().Count();
        Assert.True(
            distinctPartitions > 1,
            $"Expected messages on multiple partitions, but all landed on {distinctPartitions} partition(s)");
    }

    public sealed class PartitionCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<string> PartitionIds { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            if (context.Features.TryGet<EventHubReceiveFeature>(out var feature))
            {
                PartitionIds.Add(feature.PartitionId);
            }
            else
            {
                PartitionIds.Add("unknown");
            }
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class PartitionSpyConsumer(PartitionCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
