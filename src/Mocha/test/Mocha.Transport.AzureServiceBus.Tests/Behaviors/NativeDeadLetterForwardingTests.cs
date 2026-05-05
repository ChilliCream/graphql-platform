using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class NativeDeadLetterForwardingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public NativeDeadLetterForwardingTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UseNativeDeadLetterForwarding_Should_SetForwardDeadLetteredMessagesToOnQueue_When_Configured()
    {
        // arrange
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("dlq-fwd");
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.Endpoint("dlq-fwd-ep")
                    .Handler<OrderCreatedHandler>()
                    .Queue(queueName)
                    .UseNativeDeadLetterForwarding();
            })
            .BuildTestBusAsync();

        // act
        var transport = GetTransport(bus);
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var queue = topology.Queues.First(q => q.Name == queueName);

        // assert
        Assert.Equal($"{queueName}_error", queue.ForwardDeadLetteredMessagesTo);

        // and the broker reflects it
        var adminClient = new ServiceBusAdministrationClient(ctx.ConnectionString);
        var properties = await adminClient.GetQueueAsync(queueName);
        Assert.Equal($"{queueName}_error", properties.Value.ForwardDeadLetteredMessagesTo);
    }

    [Fact]
    public async Task UseNativeDeadLetterForwarding_Should_ThrowAtProvisioning_When_CustomForwardingAlreadyConfigured()
    {
        // arrange
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("dlq-conflict");

        // act & assert - building the bus should surface the conflict thrown by the topology convention
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var bus = await new ServiceCollection()
                .AddMessageBus()
                .AddEventHandler<OrderCreatedHandler>()
                .AddAzureServiceBus(t =>
                {
                    t.ConnectionString(ctx.ConnectionString);
                    t.DeclareQueue(queueName).WithForwardDeadLetteredMessagesTo("custom-dlq");
                    t.Endpoint("dlq-conflict-ep")
                        .Handler<OrderCreatedHandler>()
                        .Queue(queueName)
                        .UseNativeDeadLetterForwarding();
                })
                .BuildTestBusAsync();
        });

        Assert.Contains("UseNativeDeadLetterForwarding", ex.Message);
        Assert.Contains("custom-dlq", ex.Message);
    }

    [Fact]
    public async Task UseNativeDeadLetterForwarding_Should_RouteBrokerDeadLettersToErrorQueue_When_MaxDeliveryCountExceeded()
    {
        // arrange
        var capture = new ErrorCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("max-dl");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<AlwaysThrowingHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                // MaxDeliveryCount = 1 -> first failed delivery moves the message to the broker DLQ,
                // which is then forwarded into the Mocha-managed _error queue by the queue's
                // ForwardDeadLetteredMessagesTo binding.
                t.DeclareQueue(queueName).WithMaxDeliveryCount(1);
                t.Endpoint("max-dl-ep")
                    .Handler<AlwaysThrowingHandler>()
                    .Queue(queueName)
                    .UseNativeDeadLetterForwarding();
                t.Endpoint("max-dl-error-ep")
                    .Queue($"{queueName}_error")
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<ErrorSpyConsumer>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DL" }, CancellationToken.None);

        // assert - the message is observable on the _error queue, having flowed through the broker DLQ
        Assert.True(
            await capture.WaitAsync(s_timeout),
            "Error queue consumer did not receive the broker-dead-lettered message");
        Assert.Equal("ORD-DL", Assert.Single(capture.Messages).OrderId);
    }

    private static AzureServiceBusMessagingTransport GetTransport(TestBus bus)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        return runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
    }

    public sealed class ErrorCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];

        public void Record(OrderCreated message)
        {
            Messages.Add(message);
            _semaphore.Release();
        }

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphore.WaitAsync(timeout);
    }

    public sealed class AlwaysThrowingHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler always throws to trigger broker dead-lettering");
        }
    }

    public sealed class ErrorSpyConsumer(ErrorCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context.Message);
            return default;
        }
    }
}
