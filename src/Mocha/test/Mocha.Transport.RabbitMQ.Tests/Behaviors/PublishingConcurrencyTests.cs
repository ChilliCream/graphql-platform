using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class PublishingConcurrencyTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_StayWithinChannelLimit_When_ThousandsSendConcurrently()
    {
        // arrange
        const int count = 3000;
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 128;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("burst");
                t.DispatchEndpoint("burst-send").ToQueue("burst").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

        // act
        var sends = Enumerable.Range(0, count)
            .Select(i => SendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }, TimeSpan.FromSeconds(30)));
        await Task.WhenAll(sends);

        // assert
        Assert.Equal(count, await vhost.GetMessageCountAsync("burst"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_Should_Deliver_When_PreviousSendsWereCancelled(bool warmup)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 128;
        var payments = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(payments)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("cancelled-sends").Handler<PaymentHandler>();
                t.DispatchEndpoint("cancelled-send").ToQueue("cancelled-sends").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        if (warmup)
        {
            await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        }

        // act
        await using (await fixture.BlockPublishersAsync())
        {
            for (var wave = 0; wave < 5; wave++)
            {
                await Task.WhenAll(Enumerable.Range(0, 100).Select(i =>
                    Assert.ThrowsAnyAsync<OperationCanceledException>(
                        () => SendAsync(
                            bus,
                            new ProcessPayment { OrderId = $"{wave}-{i}", Amount = 1 },
                            TimeSpan.FromMilliseconds(300)))));
            }
        }

        await SendAsync(bus, new ProcessPayment { OrderId = "resumed", Amount = 1 });

        // assert
        Assert.True(await Condition.WaitAsync(
            () => payments.Messages.Cast<ProcessPayment>().Any(p => p.OrderId == "resumed"),
            s_timeout));
        Assert.Equal(1m, Assert.Single(payments.Messages.Cast<ProcessPayment>(), p => p.OrderId == "resumed").Amount);
        Assert.Equal(2, await vhost.GetConnectionCountAsync());
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverEveryMessage_When_FirstPublishesRunConcurrently()
    {
        // arrange
        const int count = 25;
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await Task.WhenAll(Enumerable.Range(0, count).Select(i => messageBus.PublishAsync(
            new OrderCreated { OrderId = $"ORD-{i}" },
            TestContext.Current.CancellationToken).AsTask()));

        // assert
        Assert.True(await orders.WaitAsync(s_timeout, count));
        Assert.Equal(
            Enumerable.Range(0, count).Select(i => $"ORD-{i}").Order(),
            orders.Messages.Cast<OrderCreated>().Select(order => order.OrderId).Order());
    }

    private static async Task SendAsync(TestBus bus, ProcessPayment message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? s_timeout);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
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
