using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class BatchDispatchTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public BatchDispatchTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_BatchModeEnabled()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("batch");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .ConfigureDefaults(d => d.DefaultBatchMode = EventHubBatchMode.Batch)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 99.99m },
            CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the request");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-1", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverAllMessages_When_MultipleSentInBatchMode()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("batch");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        const int messageCount = 20;
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .ConfigureDefaults(d => d.DefaultBatchMode = EventHubBatchMode.Batch)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < messageCount; i++)
        {
            await messageBus.SendAsync(
                new ProcessPayment { OrderId = $"ORD-{i}", Amount = i * 10m },
                CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not receive all {messageCount} messages within timeout");

        Assert.Equal(messageCount, recorder.Messages.Count);

        var ids = recorder
            .Messages.Cast<ProcessPayment>()
            .Select(m => m.OrderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(messageCount, ids.Distinct().Count());
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToHandler_When_BatchModeOnEndpoint()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("batch");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .ConfigureDefaults(d => d.DefaultBatchMode = EventHubBatchMode.Batch)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-1" },
            CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-1", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverConcurrently_When_BatchModeWithParallelSends()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("batch");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        const int messageCount = 50;
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .ConfigureDefaults(d => d.DefaultBatchMode = EventHubBatchMode.Batch)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send concurrently
        var tasks = Enumerable.Range(0, messageCount)
            .Select(i => messageBus.SendAsync(
                new ProcessPayment { OrderId = $"ORD-{i}", Amount = i },
                CancellationToken.None).AsTask());

        await Task.WhenAll(tasks);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: messageCount),
            $"Handler did not receive all {messageCount} messages within timeout");

        Assert.Equal(messageCount, recorder.Messages.Count);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
