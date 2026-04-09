using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class ConsumerGroupIsolationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public ConsumerGroupIsolationTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_Should_DeliverToAllConsumerGroups_When_MultipleGroupsSubscribed()
    {
        // arrange
        var recorderA = new MessageRecorder();
        var recorderB = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("partition");
        var groupA = _fixture.GetUniqueConsumerGroup();
        var groupB = _fixture.GetUniqueConsumerGroup();

        // Bus A - consumes from group-a
        await using var busA = await new ServiceCollection()
            .AddSingleton(recorderA)
            .AddMessageBus()
            .AddEventHandler<OrderHandlerA>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(groupA))
            .BuildTestBusAsync();

        // Bus B - consumes from group-b
        await using var busB = await new ServiceCollection()
            .AddSingleton(recorderB)
            .AddMessageBus()
            .AddEventHandler<OrderHandlerB>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(groupB))
            .BuildTestBusAsync();

        // Publish from bus A
        using var scope = busA.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-CG" }, CancellationToken.None);

        // assert - both consumer groups should receive the event
        Assert.True(
            await recorderA.WaitAsync(s_timeout),
            "Consumer group A did not receive the event");
        Assert.True(
            await recorderB.WaitAsync(s_timeout),
            "Consumer group B did not receive the event");

        Assert.Single(recorderA.Messages);
        Assert.Single(recorderB.Messages);
    }

    [Fact]
    public async Task Publish_Should_DeliverAllMessages_When_MultipleMessagesAndMultipleGroups()
    {
        // arrange
        var recorderA = new MessageRecorder();
        var recorderB = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("partition");
        var groupA = _fixture.GetUniqueConsumerGroup();
        var groupB = _fixture.GetUniqueConsumerGroup();
        const int messageCount = 5;

        await using var busA = await new ServiceCollection()
            .AddSingleton(recorderA)
            .AddMessageBus()
            .AddEventHandler<OrderHandlerA>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(groupA))
            .BuildTestBusAsync();

        await using var busB = await new ServiceCollection()
            .AddSingleton(recorderB)
            .AddMessageBus()
            .AddEventHandler<OrderHandlerB>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(groupB))
            .BuildTestBusAsync();

        using var scope = busA.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert - both groups should receive all messages
        Assert.True(
            await recorderA.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Consumer group A did not receive all {messageCount} messages");
        Assert.True(
            await recorderB.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Consumer group B did not receive all {messageCount} messages");

        Assert.Equal(messageCount, recorderA.Messages.Count);
        Assert.Equal(messageCount, recorderB.Messages.Count);
    }

    public sealed class OrderHandlerA(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class OrderHandlerB(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
