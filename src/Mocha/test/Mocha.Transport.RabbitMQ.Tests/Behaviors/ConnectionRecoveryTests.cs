using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var logger = new CapturingLogger<RabbitMQConsumerManager>();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddLogging(b => b.AddProvider(logger))
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // drop all connections
        await _fixture.CloseAllConnectionsAsync("recovery-test");

        // verify delivery after recovery
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 2);

        // assert
        Assert.Equal(["ORD-1", "ORD-2"], GetOrderIds(recorder));
        Assert.True(await Condition.WaitAsync(
            () => logger.Entries.Any(e => e.Event == "ConnectionRecovered"), s_recoveryTimeout));
        Assert.Equal(
            [(LogLevel.Warning, "ConnectionShutdownDetected"), (LogLevel.Information, "ConnectionRecovered")],
            logger.Entries
                .Where(e => e.Event is "ConnectionShutdownDetected" or "ConnectionRecovered")
                .Select(e => (e.Level, e.Event)));
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

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive message 1");

        // drop connections twice
        await _fixture.CloseAllConnectionsAsync("recovery-test-1");
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 2);
        Assert.Equal(["ORD-1", "ORD-2"], GetOrderIds(recorder));
        await _fixture.CloseAllConnectionsAsync("recovery-test-2");

        // wait for recovery and verify message 3
        await WaitForRecoveryAsync(messageBus, recorder, orderNumber: 3);

        // assert
        Assert.Equal(["ORD-1", "ORD-2", "ORD-3"], GetOrderIds(recorder));
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
        Assert.Equal(
            ["ORD-1", "ORD-2"],
            recorder.Messages.Cast<ProcessPayment>().Select(p => p.OrderId).Distinct().Order());
    }

    private static async Task WaitForRecoveryAsync(IMessageBus messageBus, MessageRecorder recorder, int orderNumber)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_recoveryTimeout);

        await messageBus.PublishAsync(new OrderCreated { OrderId = $"ORD-{orderNumber}" }, cts.Token);
        Assert.True(await Condition.WaitAsync(
            () => GetOrderIds(recorder).Contains($"ORD-{orderNumber}"), s_recoveryTimeout));
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
        Assert.True(await Condition.WaitAsync(
            () => recorder.Messages.Cast<ProcessPayment>().Any(p => p.OrderId == $"ORD-{orderNumber}"),
            s_recoveryTimeout));
    }

    private static IEnumerable<string> GetOrderIds(MessageRecorder recorder)
        => recorder.Messages.Cast<OrderCreated>().Select(order => order.OrderId).Distinct().Order();

    public sealed class PaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
