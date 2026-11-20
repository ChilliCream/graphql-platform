using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.Dispatcher)]
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
        lease.IsReusable = true;
        lease.Dispose();
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
        var connection = await dispatcher.GetConnectionAsync();
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.RentChannelAsync(deadline.Token).AsTask());
        var channelsStayedOpen = first.Channel.IsOpen && second.Channel.IsOpen;
        first.IsReusable = true;
        first.Dispose();
        second.IsReusable = true;
        second.Dispose();
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var current = await dispatcher.GetConnectionAsync();

        // assert
        Assert.True(channelsStayedOpen);
        Assert.Same(connection, current);
        Assert.Equal(1, (await vhost.Broker.GetQueueAsync("payments"))?.Messages);
    }

    [Fact]
    public async Task RentChannelAsync_Should_Reconnect_When_PublisherChannelOpenTimesOut()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(30);
        await using var dispatcher = CreateDispatcher(
            vhost,
            new RabbitMQDispatcherOptions { ChannelOpenTimeout = TimeSpan.FromSeconds(1) });
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        IConnection original;
        TimeoutException exception;

        // the lease is not reusable, so the next rent opens a channel
        using (var lease = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken))
        {
            original = lease.Connection;
        }

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            exception = await Assert.ThrowsAsync<TimeoutException>(
                () => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
                    .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));
        }

        await AssertCanPublishAsync(dispatcher, vhost);
        var current = await dispatcher.GetConnectionAsync();

        // assert
        Assert.Equal("The broker did not answer the request to open a channel.", exception.Message);
        Assert.NotSame(original, current);
    }

    [Fact]
    public async Task RentChannelAsync_Should_PreserveOtherPublishes_When_ChannelAllocationFails()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var connection = await dispatcher.GetConnectionAsync();
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
            rented.IsReusable = true;
            rented.Dispose();
        }

        // assert
        Assert.True(connection.IsOpen);
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
        rented.IsReusable = true;
        rented.Dispose();
        await vhost.Broker.WaitForNoConnectionAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_FailPendingRent_When_ChannelIsOpening()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var dispatcher = CreateDispatcher(vhost);
        await dispatcher.StartAsync(TestContext.Current.CancellationToken);
        Task<ChannelLease> pending;

        // act
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            pending = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();
            await dispatcher.DisposeAsync().AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        }

        // assert
        // the broker lists the connection for another 30 seconds after the dispose gave up on closing it
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pending);
        await vhost.Broker.WaitForNoConnectionAsync(TestTimeouts.Signal + TimeSpan.FromSeconds(15));
    }

    private static RabbitMQDispatcher CreateDispatcher(VhostContext vhost)
        => CreateDispatcher(vhost, new RabbitMQDispatcherOptions());

    private static RabbitMQDispatcher CreateDispatcher(VhostContext vhost, RabbitMQDispatcherOptions options)
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
            },
            options);

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
            lease.IsReusable = confirmed;
            lease.Dispose();
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
