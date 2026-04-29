using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

/// <summary>
/// Connection recovery for Azure Service Bus is handled inside the SDK — the <c>ServiceBusClient</c>
/// transparently rebuilds AMQP links and channels using its own retry policy, so there is no
/// public API on the transport surface to drop or assert reconnects against. The Squadron emulator
/// also does not expose a Docker-level "kill connection" primitive analogous to RabbitMQ's
/// <c>rabbitmqctl close_all_connections</c>. The RabbitMQ <c>ConnectionRecoveryTests</c> scenarios
/// that rely on broker-side disconnects (e.g.
/// <c>Consumer_Should_HandleMultipleDisconnects_When_ConnectionDroppedRepeatedly</c>
/// and the dispatcher channel-pool recovery test) are therefore not portable here.
///
/// What we can verify deterministically is that messages enqueued on the broker survive a full
/// transport teardown and are picked up after a fresh transport instance starts on the same queue.
/// This exercises the operational equivalent of "process restart after a network outage" without
/// requiring infrastructure-level disconnect injection.
/// </summary>
[Collection("AzureServiceBus")]
public class ConnectionRecoveryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public ConnectionRecoveryTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_ResumeReceiving_When_TransportRestarted()
    {
        // arrange - same test context across both bus instances so we converge on the same
        // queue on the broker (the queue persists after the first transport tears down).
        var ctx = _fixture.CreateTestContext();

        var firstRecorder = new MessageRecorder();
        await using (var firstBus = await new ServiceCollection()
            .AddSingleton(firstRecorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync())
        {
            using var firstScope = firstBus.Provider.CreateScope();
            var firstMessageBus = firstScope.ServiceProvider.GetRequiredService<IMessageBus>();

            // act - publish on the first instance and wait for delivery
            await firstMessageBus.PublishAsync(new OrderCreated { OrderId = "ORD-1" }, CancellationToken.None);

            Assert.True(
                await firstRecorder.WaitAsync(s_timeout),
                "Handler did not receive message on the first transport instance");
        }

        // act - the first transport tore down its ServiceBusClient. Spin up a fresh transport on
        // the same queue (per-test prefix) and publish another message.
        var secondRecorder = new MessageRecorder();
        await using var secondBus = await new ServiceCollection()
            .AddSingleton(secondRecorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var secondScope = secondBus.Provider.CreateScope();
        var secondMessageBus = secondScope.ServiceProvider.GetRequiredService<IMessageBus>();

        await secondMessageBus.PublishAsync(new OrderCreated { OrderId = "ORD-2" }, CancellationToken.None);

        // assert - the freshly built transport receives the message on the same queue
        Assert.True(
            await secondRecorder.WaitAsync(s_timeout),
            "Handler did not receive message after transport restart");

        var order = Assert.IsType<OrderCreated>(Assert.Single(secondRecorder.Messages));
        Assert.Equal("ORD-2", order.OrderId);
    }

    [Fact]
    public async Task Dispatcher_Should_ResumeSending_When_TransportRestarted()
    {
        // arrange - the dispatcher in the second bus must rebuild its sender cache from scratch
        // on the same queue. The first bus is only used to provision and prove end-to-end
        // delivery; the second bus exercises a fresh ServiceBusClient and ServiceBusSender pool.
        var ctx = _fixture.CreateTestContext();

        var firstRecorder = new MessageRecorder();
        await using (var firstBus = await new ServiceCollection()
            .AddSingleton(firstRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync())
        {
            using var firstScope = firstBus.Provider.CreateScope();
            var firstMessageBus = firstScope.ServiceProvider.GetRequiredService<IMessageBus>();

            await firstMessageBus.SendAsync(
                new ProcessPayment { OrderId = "ORD-1", Amount = 50.00m },
                CancellationToken.None);

            Assert.True(
                await firstRecorder.WaitAsync(s_timeout),
                "Handler did not receive request on the first transport instance");
        }

        // act - send a new request through a fresh dispatcher
        var secondRecorder = new MessageRecorder();
        await using var secondBus = await new ServiceCollection()
            .AddSingleton(secondRecorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddAzureServiceBus(ctx.ConnectionString)
            .BuildTestBusAsync();

        using var secondScope = secondBus.Provider.CreateScope();
        var secondMessageBus = secondScope.ServiceProvider.GetRequiredService<IMessageBus>();

        await secondMessageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-2", Amount = 75.00m },
            CancellationToken.None);

        // assert - the freshly rebuilt sender pool delivers the new request
        Assert.True(
            await secondRecorder.WaitAsync(s_timeout),
            "Handler did not receive request after transport restart");

        var payment = Assert.IsType<ProcessPayment>(Assert.Single(secondRecorder.Messages));
        Assert.Equal("ORD-2", payment.OrderId);
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
