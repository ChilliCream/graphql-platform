using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class RabbitMQDispatcherIntegrationTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task SendAsync_Should_WaitForChannel_When_EveryChannelIsLeased()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("leased");
                t.DispatchEndpoint("leased-send").ToQueue("leased").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Transport.Dispatcher;
        var lease = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        var sends = Enumerable.Range(0, 8)
            .Select(i => SendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }))
            .ToArray();
        var whileLeased = await vhost.Broker.GetQueueAsync("leased");
        var allWaiting = sends.All(send => !send.IsCompleted);
        dispatcher.Return(lease, reusable: true);
        await Task.WhenAll(sends).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        Assert.True(allWaiting);
        Assert.Equal(1, whileLeased?.Messages);
        Assert.Equal(9, (await vhost.Broker.GetQueueAsync("leased"))?.Messages);
        Assert.Equal(1, (await vhost.Broker.GetStateAsync()).IdleChannels);
    }

    [Fact]
    public async Task SendAsync_Should_Complete_When_AnotherSendIsCancelledWhileTheBrokerDoesNotAnswer()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("payments");
                t.DispatchEndpoint("payment-send").ToQueue("payments").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        using var cancellation = new CancellationTokenSource();
        using var scope = bus.Provider.CreateScope();
        Task surviving;

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            var cancelled = scope.ServiceProvider.GetRequiredService<IMessageBus>()
                .SendAsync(new ProcessPayment { OrderId = "cancelled", Amount = 1 }, cancellation.Token).AsTask();
            surviving = SendAsync(bus, new ProcessPayment { OrderId = "surviving", Amount = 1 });
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        }

        await surviving.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        // the cancelled message is in the queue as well when it was written before the cancellation
        Assert.InRange((await vhost.Broker.GetQueueAsync("payments"))?.Messages ?? 0, 2, 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishAsync_Should_WaitForTopology_When_PublisherConnectionRecovers(bool automaticRecoveryEnabled)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        vhost.ConnectionSettings.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
        var recoveryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRecovery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var setups = 0;
        await using var dispatcher = TestManagers.CreateDispatcher(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct),
            async (connection, ct) =>
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                if (Interlocked.Increment(ref setups) > 1)
                {
                    recoveryStarted.TrySetResult();
                    await releaseRecovery.Task.WaitAsync(ct);
                }

                await channel.ExchangeDeclareAsync("events", "fanout", durable: true, cancellationToken: ct);
                await channel.QueueDeclareAsync(
                    "orders",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct);
                await channel.QueueBindAsync("orders", "events", "", cancellationToken: ct);
            });
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        await vhost.Faults.DeleteQueueAsync("orders");

        // act
        await vhost.Faults.CloseConnectionsAsync();
        await recoveryStarted.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        var publish = PublishToAsync(dispatcher, exchange: "events", routingKey: "");
        bool publishWaited;
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.RentChannelAsync(deadline.Token).AsTask());

            publishWaited = !publish.IsCompleted;
        }
        finally
        {
            releaseRecovery.TrySetResult();
            await publish.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.True(publishWaited);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("orders"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_KeepPublisherConnection_When_NegotiatedChannelLimitReached()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("payments");
                t.DispatchEndpoint("payment-send").ToQueue("payments").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        var dispatcher = bus.Transport.Dispatcher;
        var connection = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.RentChannelAsync(deadline.Token).AsTask());
        var channelsStayedOpen = first.Channel.IsOpen && second.Channel.IsOpen;
        dispatcher.Return(first, reusable: true);
        dispatcher.Return(second, reusable: true);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(channelsStayedOpen);
        Assert.Same(connection, current);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_StayWithinChannelLimit_When_ChannelRentIsCancelledRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 3;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("pending-channel");
                t.DispatchEndpoint("pending-send").ToQueue("pending-channel").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        var dispatcher = bus.Transport.Dispatcher;

        // act
        // the first three rents each leave a channel that is still opening, the other two wait for one of them
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            for (var i = 0; i < 5; i++)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => dispatcher.RentChannelAsync(timeout.Token).AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            }
        }

        await SendAsync(bus, new ProcessPayment { OrderId = "resumed", Amount = 1 });
        var state = await Wait.UntilAsync(
            vhost.Broker.GetStateAsync,
            state => state.IdleChannels == 3,
            "the three channels that were opening to be open and idle");

        // assert
        Assert.Equal(2, state.Connections);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("pending-channel"))?.Messages);
    }

    [Fact]
    public async Task SendAsync_Should_Reconnect_When_PublisherChannelOpenTimesOut()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(30);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("channel-timeout");
                t.DispatchEndpoint("timeout-send").ToQueue("channel-timeout").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        dispatcher.ChannelOpenTimeout = TimeSpan.FromMilliseconds(250);
        TimeoutException exception;

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            exception = await Assert.ThrowsAsync<TimeoutException>(
                () => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
                    .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));
        }

        dispatcher.ChannelOpenTimeout = TestTimeouts.Signal;
        await SendAsync(bus, new ProcessPayment { OrderId = "resumed", Amount = 1 });
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("The broker did not answer the request to open a channel.", exception.Message);
        Assert.NotSame(original, current);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("channel-timeout"))?.Messages);
    }

    [Fact]
    public async Task RentChannelAsync_Should_PreserveOtherPublishes_When_ChannelAllocationFails()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var connection = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var external = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        try
        {
            await Assert.ThrowsAsync<ChannelAllocationException>(
                () => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask());
        }
        finally
        {
            await external.DisposeAsync();
            dispatcher.Return(rented, reusable: true);
        }

        // assert
        Assert.True(connection.IsOpen);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task RentChannelAsync_Should_WaitForChannelClose_When_ChannelIsNotReusable()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            dispatcher.Return(first, reusable: false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.RentChannelAsync(deadline.Token).AsTask());
        }

        var replacement = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        dispatcher.Return(replacement, reusable: true);
        dispatcher.Return(second, reusable: true);

        // assert
        Assert.NotSame(first.Channel, replacement.Channel);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task RentChannelAsync_Should_UseNewCapacity_When_ConnectionReplacedWhileWaiting()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var waiting = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();

        // act
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await original.AbortAsync(TestContext.Current.CancellationToken);
        var second = await waiting.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        var third = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        dispatcher.Return(first, reusable: true);
        dispatcher.Return(second, reusable: true);
        dispatcher.Return(third, reusable: true);

        // assert
        Assert.NotSame(second.Channel, third.Channel);
        Assert.NotSame(original, second.Connection);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task DisposeAsync_Should_FailWaitingRent_When_AllChannelsAreLeased()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var waiting = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();

        // act
        await dispatcher.DisposeAsync();

        // assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => waiting);
        dispatcher.Return(rented, reusable: true);
        await vhost.Broker.WaitForNoConnectionAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_FailPendingRent_When_ChannelIsOpening()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        Task<PublisherLease> pending;

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            pending = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();
            await dispatcher.DisposeAsync().AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        }

        // assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pending);
        await vhost.Broker.WaitForNoConnectionAsync();
    }

    private static RabbitMQDispatcher CreateDispatcher(VhostContext vhost)
        => TestManagers.CreateDispatcher(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct),
            async (connection, ct) =>
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                await channel.QueueDeclareAsync(
                    "orders",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct);
            });

    private static async Task AssertCanPublishAsync(RabbitMQDispatcher dispatcher, VhostContext vhost)
    {
        await PublishToAsync(dispatcher, exchange: "", routingKey: "orders");

        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var reader = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await reader.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);
        Assert.Equal("order", Encoding.UTF8.GetString(result!.Body.Span));
    }

    private static async Task PublishToAsync(RabbitMQDispatcher dispatcher, string exchange, string routingKey)
    {
        var lease = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var confirmed = false;
        try
        {
            await lease.Channel.BasicPublishAsync(
                exchange,
                routingKey,
                false,
                new BasicProperties(),
                "order"u8.ToArray(),
                TestContext.Current.CancellationToken);
            confirmed = true;
        }
        finally
        {
            dispatcher.Return(lease, reusable: confirmed);
        }
    }

    private static async Task SendAsync(TestBus bus, ProcessPayment message)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
    }
}
