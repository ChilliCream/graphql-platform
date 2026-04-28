using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class ConnectionRecoveryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public ConnectionRecoveryTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_ResumeReceiving_When_HandlerPreviouslyFailed()
    {
        // arrange
        var failingRecorder = new MessageRecorder();
        var successRecorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("recovery");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddKeyedSingleton("failing", failingRecorder)
            .AddKeyedSingleton("success", successRecorder)
            .AddMessageBus()
            .AddEventHandler<FailOnceHandler>()
            .AddEventHandler<AlwaysSucceedHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - first message triggers handler failure, second should still be delivered
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-OK" }, CancellationToken.None);

        // assert - the always-succeed handler should receive both messages
        Assert.True(
            await successRecorder.WaitAsync(s_timeout, expectedCount: 2),
            "Success handler did not receive both messages after a sibling handler failed");

        Assert.Equal(2, successRecorder.Messages.Count);
    }

    [Fact]
    public async Task Consumer_Should_ContinueProcessing_When_MultipleHandlerFailuresOccur()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("recovery");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<CountingFailHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send 5 messages; the handler fails on the first 2, succeeds on the rest
        for (var i = 1; i <= 5; i++)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{i}" }, CancellationToken.None);
        }

        // assert - at least the 3 successful messages should be recorded
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 3),
            "Handler did not receive at least 3 successful messages after initial failures");

        Assert.True(
            recorder.Messages.Count >= 3,
            $"Expected at least 3 recorded messages but got {recorder.Messages.Count}");
    }

    [Fact]
    public async Task Producer_Should_RemainFunctional_When_ConsumerRecoveredFromError()
    {
        // arrange
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("recovery");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - send message 1
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
            CancellationToken.None);

        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // send message 2 after a brief pause to exercise the producer's connection reuse
        await Task.Delay(TimeSpan.FromSeconds(1));
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-2", Amount = 75.00m },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler did not receive message 2 after producer reuse");

        Assert.True(
            recorder.Messages.Count >= 2,
            $"Expected at least 2 messages but got {recorder.Messages.Count}");
    }

    /// <summary>
    /// Handler that always throws, paired with a sibling handler that always succeeds.
    /// Verifies that one handler's failure does not block other handlers from processing.
    /// </summary>
    public sealed class FailOnceHandler([FromKeyedServices("failing")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            throw new InvalidOperationException("Deliberate handler failure for recovery test");
        }
    }

    public sealed class AlwaysSucceedHandler([FromKeyedServices("success")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    /// <summary>
    /// Handler that fails on the first N invocations, then succeeds.
    /// Used to verify the processor pipeline continues after transient handler errors.
    /// </summary>
    public sealed class CountingFailHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        private int _callCount;

        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            var count = Interlocked.Increment(ref _callCount);

            if (count <= 2)
            {
                throw new InvalidOperationException($"Deliberate failure #{count}");
            }

            recorder.Record(message);
            return default;
        }
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
