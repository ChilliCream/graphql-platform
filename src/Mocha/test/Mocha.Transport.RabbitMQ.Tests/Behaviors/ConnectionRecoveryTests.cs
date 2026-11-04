using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public class ConnectionRecoveryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_recoveryTimeout = TimeSpan.FromSeconds(15);
    private readonly RabbitMQFixture _fixture;

    public ConnectionRecoveryTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_ResumeReceiving_When_ConnectionRecovered()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish message 1, verify received
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // drop all connections
        await _fixture.CloseAllConnectionsAsync("recovery-test");

        // verify delivery after recovery
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 2);

        // assert - message 2 should have been received after recovery
        Assert.True(recorder.Messages.Count >= 2, $"Expected at least 2 messages but got {recorder.Messages.Count}");
    }

    [Fact]
    public async Task Consumer_Should_HandleMultipleDisconnects_When_ConnectionDroppedRepeatedly()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish message 1
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // drop connections twice
        await _fixture.CloseAllConnectionsAsync("recovery-test-1");
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 2);
        Assert.True(recorder.Messages.Count >= 2, $"Expected at least 2 messages but got {recorder.Messages.Count}");
        await _fixture.CloseAllConnectionsAsync("recovery-test-2");

        // wait for recovery and verify message 3
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 3);

        // assert
        Assert.True(recorder.Messages.Count >= 3, $"Expected at least 3 messages but got {recorder.Messages.Count}");
    }

    [Fact]
    public async Task ChannelPool_Should_RecoverGracefully_When_ConnectionLost()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // send message 1
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
            TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // drop connections
        await _fixture.CloseAllConnectionsAsync("channel-pool-recovery");

        // wait for recovery, then send again
        await WaitForDispatchRecoveryAsync(messageBus, recorder, orderNumber: 2);

        // assert
        Assert.True(recorder.Messages.Count >= 2, $"Expected at least 2 messages but got {recorder.Messages.Count}");
    }

    private static async Task WaitForRecoveryAsync(IMessageBus messageBus, MessageRecorder recorder, int orderNumber)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_recoveryTimeout);

        await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{orderNumber}" }, cts.Token);
        Assert.True(await recorder.WaitAsync(s_recoveryTimeout));
    }

    private static async Task WaitForDispatchRecoveryAsync(
        IMessageBus messageBus,
        MessageRecorder recorder,
        int orderNumber)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_recoveryTimeout);

        await messageBus.SendAsync(
            new ProcessPayment { OrderId = $"ORD-{orderNumber}", Amount = 1.00m },
            cts.Token);
        Assert.True(await recorder.WaitAsync(s_recoveryTimeout));
    }

    public sealed class PaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
