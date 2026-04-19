using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class NativeDeadLetterApiTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly AzureServiceBusFixture _fixture;

    public NativeDeadLetterApiTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterAsync_Should_MoveMessageToDeadLetterQueue_When_CalledFromHandler()
    {
        // arrange
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("dl-api");
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<DeadLetteringConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("dl-api-ep").Consumer<DeadLetteringConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "DL-1" }, CancellationToken.None);

        // assert - the message should land in the broker DLQ with our reason and description
        var dlqMessage = await ReceiveFromDeadLetterAsync(ctx.ConnectionString, queueName, s_timeout);
        Assert.NotNull(dlqMessage);
        Assert.Equal("InvalidPayload", dlqMessage!.DeadLetterReason);
        Assert.Equal("Missing customer id for DL-1", dlqMessage.DeadLetterErrorDescription);
    }

    [Fact]
    public async Task AbandonAsync_Should_IncrementDeliveryCount_When_CalledWithProperties()
    {
        // arrange
        var capture = new DeliveryCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("abandon-api");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<AbandoningConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                // Allow at least 2 deliveries so the message redelivers after our abandon.
                t.DeclareQueue(queueName).WithMaxDeliveryCount(5);
                t.Endpoint("abandon-api-ep").Consumer<AbandoningConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ABN-1" }, CancellationToken.None);

        // assert - we should observe at least two deliveries; the second carries the property we set.
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: 2),
            "Did not observe a redelivery after abandon");

        var deliveries = capture.Deliveries.OrderBy(d => d.DeliveryCount).ToList();
        Assert.True(deliveries[1].DeliveryCount > deliveries[0].DeliveryCount);
        Assert.True(
            deliveries[1].HasAbandonMarker,
            "Redelivered message should carry the abandon-marker property set by AbandonAsync");
    }

    [Fact]
    public async Task AcknowledgementMiddleware_Should_NotThrow_When_HandlerAlreadyDeadLettered()
    {
        // arrange - a handler that dead-letters then returns normally exercises the idempotent-Complete
        // path in the acknowledgement middleware. If MessageLockLost escapes, the processor's error
        // handler fires and the test will record the failure.
        var processorErrors = new ConcurrentBag<Exception>();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("idempotent");
        await using var bus = await new ServiceCollection()
            .AddSingleton(processorErrors)
            .AddMessageBus()
            .AddConsumer<DeadLetteringConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("idempotent-ep").Consumer<DeadLetteringConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "IDEMP-1" }, CancellationToken.None);

        // wait for the message to surface in the DLQ — proves the handler ran and the outer
        // Complete did not surface MessageLockLost as a fault that prevented dead-lettering.
        var dlqMessage = await ReceiveFromDeadLetterAsync(ctx.ConnectionString, queueName, s_timeout);

        // assert
        Assert.NotNull(dlqMessage);
        Assert.Equal("InvalidPayload", dlqMessage!.DeadLetterReason);

        // The processor never had an unhandled MessageLockLost — the idempotent catch swallows it.
        Assert.DoesNotContain(
            processorErrors,
            e => e is ServiceBusException sbe && sbe.Reason == ServiceBusFailureReason.MessageLockLost);
    }

    private static async Task<ServiceBusReceivedMessage?> ReceiveFromDeadLetterAsync(
        string connectionString,
        string queueName,
        TimeSpan timeout)
    {
        await using var client = new ServiceBusClient(connectionString);
        await using var receiver = client.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        return await receiver.ReceiveMessageAsync(timeout);
    }

    public sealed class DeadLetteringConsumer : IConsumer<OrderCreated>
    {
        public async ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            await context.AzureServiceBus().DeadLetterAsync(
                "InvalidPayload",
                $"Missing customer id for {context.Message.OrderId}");
        }
    }

    public sealed class DeliveryCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<DeliveryRecord> Deliveries { get; } = [];

        public void Record(int deliveryCount, bool hasAbandonMarker)
        {
            Deliveries.Add(new DeliveryRecord(deliveryCount, hasAbandonMarker));
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

    public sealed record DeliveryRecord(int DeliveryCount, bool HasAbandonMarker);

    public sealed class AbandoningConsumer(DeliveryCapture capture) : IConsumer<OrderCreated>
    {
        private const string MarkerKey = "abandon-marker";

        public async ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            var asb = context.AzureServiceBus();
            var hasMarker = asb.Message.ApplicationProperties.ContainsKey(MarkerKey);
            capture.Record(asb.DeliveryCount, hasMarker);

            if (!hasMarker)
            {
                // First delivery: stamp a marker and abandon so the broker redelivers.
                await asb.AbandonAsync(new Dictionary<string, object> { [MarkerKey] = "abandoned-once" });
            }
            // On the second delivery the marker is set, so we just return and the ack MW completes.
        }
    }
}
