using System.Diagnostics;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class PublishStateTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_Throw_When_BrokerRejectsMessage()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("bounded")
                    .WithArgument("x-max-length", 1)
                    .WithArgument("x-overflow", "reject-publish");
                t.DispatchEndpoint("bounded-send").ToQueue("bounded").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

        // act
        var first = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var second = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });
        var depth = await vhost.GetMessageCountAsync("bounded");

        // assert
        new { First = first, Second = second, Depth = depth }.MatchInlineSnapshot(
            """
            {
              "First": "sent",
              "Second": "PublishException",
              "Depth": 1
            }
            """);
    }

    [Fact]
    public async Task SendAsync_Should_Deliver_When_PreviousPublishMadeBrokerCloseChannel()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var payments = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(payments)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
                t.Endpoint("payments").Handler<PaymentHandler>();
            })
            .BuildTestBusAsync();
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        Assert.True(await payments.WaitAsync(s_timeout));
        await DeleteExchangeAsync(vhost, "orders");

        // act
        // publishing to an exchange that no longer exists makes the broker close the channel with 404
        var publish = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var send = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-3", Amount = 1 });
        var received = await payments.WaitAsync(s_timeout);

        // assert
        new { Publish = publish, Send = send, Received = received }.MatchInlineSnapshot(
            """
            {
              "Publish": "AlreadyClosedException",
              "Send": "sent",
              "Received": true
            }
            """);
    }

    [Fact]
    public async Task PublishAsync_Should_NotReportSuccess_When_ConnectionBlocked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        string whileBlocked;
        TimeSpan elapsed;
        await using (await fixture.BlockPublishersAsync())
        {
            var stopwatch = Stopwatch.StartNew();
            whileBlocked = await TryPublishAsync(
                bus,
                new OrderCreated { OrderId = "ORD-2" },
                TimeSpan.FromSeconds(2));
            elapsed = stopwatch.Elapsed;
        }

        var deliveredAfterUnblock = await orders.WaitAsync(s_timeout);

        // assert
        new
        {
            WhileBlocked = whileBlocked,
            ReturnedWhenCancelled = elapsed < TimeSpan.FromSeconds(5),
            DeliveredAfterUnblock = deliveredAfterUnblock
        }.MatchInlineSnapshot(
            """
            {
              "WhileBlocked": "TaskCanceledException",
              "ReturnedWhenCancelled": true,
              "DeliveredAfterUnblock": true
            }
            """);
    }

    private static async Task DeleteExchangeAsync(VhostContext vhost, string exchange)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.ExchangeDeleteAsync(exchange, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task<string> TrySendAsync(TestBus bus, ProcessPayment message)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
            return "sent";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
    }

    private static async Task<string> TryPublishAsync(TestBus bus, OrderCreated message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? s_timeout);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message, cts.Token);
            return "sent";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
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
