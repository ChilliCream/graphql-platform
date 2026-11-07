using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class RabbitMQDispatcherIntegrationTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_PipelineConfirms_When_PublishesShareAnEndpoint()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("pipelined");
                t.DispatchEndpoint("pipelined-send").ToQueue("pipelined").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var runtime = bus.Provider.GetRequiredService<IMessagingRuntime>();
        var dispatcher = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        var endpoint = runtime.GetSendEndpoint(runtime.GetMessageType(typeof(ProcessPayment)));
        var channel = await dispatcher.RentChannelAsync(endpoint, TestContext.Current.CancellationToken);
        var initialSequence = await channel.GetNextPublishSequenceNumberAsync(TestContext.Current.CancellationToken);
        Task[] sends;
        bool allPublished;
        bool allWaiting;

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            sends = Enumerable.Range(0, 32)
                .Select(i => SendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }))
                .ToArray();
            allPublished = await Condition.WaitAsync(async () =>
                await channel.GetNextPublishSequenceNumberAsync(TestContext.Current.CancellationToken)
                    == initialSequence + 32, s_timeout);
            allWaiting = sends.All(t => !t.IsCompleted);
        }

        await Task.WhenAll(sends).WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(channel);

        // assert
        Assert.True(allPublished);
        Assert.True(allWaiting);
        Assert.Equal(33, await vhost.GetMessageCountAsync("pipelined"));
    }

    [Fact]
    public async Task SendAsync_Should_PreserveOtherPublish_When_SharedPublishIsCancelled()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("shared");
                t.DispatchEndpoint("shared-send").ToQueue("shared").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var runtime = bus.Provider.GetRequiredService<IMessagingRuntime>();
        var dispatcher = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        var endpoint = runtime.GetSendEndpoint(runtime.GetMessageType(typeof(ProcessPayment)));
        var channel = await dispatcher.RentChannelAsync(endpoint, TestContext.Current.CancellationToken);
        var initialSequence = await channel.GetNextPublishSequenceNumberAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        using var scope = bus.Provider.CreateScope();
        Task surviving;

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            var cancelled = scope.ServiceProvider.GetRequiredService<IMessageBus>()
                .SendAsync(new ProcessPayment { OrderId = "cancelled", Amount = 1 }, cancellation.Token).AsTask();
            surviving = SendAsync(bus, new ProcessPayment { OrderId = "surviving", Amount = 1 });
            Assert.True(await Condition.WaitAsync(async () =>
                await channel.GetNextPublishSequenceNumberAsync(TestContext.Current.CancellationToken)
                    == initialSequence + 2, s_timeout));
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        }

        await surviving.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(channel);

        // assert
        Assert.True(channel.IsOpen);
        Assert.Equal(3, await vhost.GetMessageCountAsync("shared"));
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
        await using var dispatcher = new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
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
                await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
                    cancellationToken: ct);
                await channel.QueueBindAsync("orders", "events", "", cancellationToken: ct);
            });
        await dispatcher.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await vhost.DeleteQueueAsync("orders");

        // act
        await fixture.CloseAllConnectionsAsync();
        await recoveryStarted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var publish = PublishAsync();
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
            await publish.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.True(publishWaited);
        Assert.Equal(1, await vhost.GetMessageCountAsync("orders"));

        async Task PublishAsync()
        {
            var channel = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
            try
            {
                await channel.BasicPublishAsync("events", "", false, new BasicProperties(), "order"u8.ToArray(),
                    TestContext.Current.CancellationToken);
            }
            finally
            {
                await dispatcher.ReturnChannelAsync(channel);
            }
        }
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
        var dispatcher = bus.Provider.GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        var connection = await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.RentChannelAsync(deadline.Token).AsTask());
        var channelsStayedOpen = first.IsOpen && second.IsOpen;
        await dispatcher.ReturnChannelAsync(first);
        await dispatcher.ReturnChannelAsync(second);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var current = await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(channelsStayedOpen);
        Assert.Same(connection, current);
        Assert.Equal(1, await vhost.GetMessageCountAsync("payments"));
    }

    [Fact]
    public async Task SendAsync_Should_ReusePendingChannel_When_ChannelRentIsCancelledRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 128;
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("pending-channel");
                t.DispatchEndpoint("pending-send").ToQueue("pending-channel").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var dispatcher = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;

        // act
        await using (await vhost.FreezeConnectionsAsync())
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
        var state = await vhost.GetStateAsync();

        // assert
        Assert.Equal(2, state.Connections);
        Assert.Equal(1, state.IdleChannels);
        Assert.Equal(1, await vhost.GetMessageCountAsync("pending-channel"));
    }

    [Fact]
    public async Task SendAsync_Should_Reconnect_When_PublisherChannelCreationTimesOut()
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
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var dispatcher = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        dispatcher.ChannelCreationTimeout = TimeSpan.FromMilliseconds(250);

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
                    .WaitAsync(s_timeout, TestContext.Current.CancellationToken));
        }

        dispatcher.ChannelCreationTimeout = s_timeout;
        await SendAsync(bus, new ProcessPayment { OrderId = "resumed", Amount = 1 });
        var recovered = await Condition.WaitAsync(async () => await vhost.GetConnectionCountAsync() == 2, s_timeout);

        // assert
        Assert.True(recovered);
        Assert.Equal(1, await vhost.GetMessageCountAsync("channel-timeout"));
    }

    [Fact]
    public async Task RentChannelAsync_Should_PreserveOtherPublishes_When_ChannelAllocationFails()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var dispatcher = CreateDispatcher(vhost);
        var connection = await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken);
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
            await dispatcher.ReturnChannelAsync(rented);
        }

        // assert
        Assert.True(connection.IsOpen);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task RentChannelAsync_Should_WaitForChannelCleanup_When_FailedChannelIsRetired()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await using var dispatcher = CreateDispatcher(vhost);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        await using (await vhost.FreezeConnectionsAsync())
        {
            dispatcher.DiscardChannel(first);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.RentChannelAsync(deadline.Token).AsTask());
        }

        var replacement = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(replacement);
        await dispatcher.ReturnChannelAsync(second);

        // assert
        Assert.NotSame(first, replacement);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task RentChannelAsync_Should_UseNewCapacity_When_ConnectionReplacedWhileWaiting()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var dispatcher = CreateDispatcher(vhost);
        var original = await dispatcher.GetConnectionAsync(TestContext.Current.CancellationToken);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var waiting = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();

        // act
        vhost.ConnectionSettings.RequestedChannelMax = 2;
        await original.AbortAsync(TestContext.Current.CancellationToken);
        await dispatcher.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var second = await waiting.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var third = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(first);
        await dispatcher.ReturnChannelAsync(second);
        await dispatcher.ReturnChannelAsync(third);

        // assert
        Assert.NotSame(second, third);
        await AssertCanPublishAsync(dispatcher, vhost);
    }

    [Fact]
    public async Task DisposeAsync_Should_CancelWaitingRent_When_AllChannelsAreLeased()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.RequestedChannelMax = 1;
        await using var dispatcher = CreateDispatcher(vhost);
        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var waiting = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();

        // act
        await dispatcher.DisposeAsync();

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        await dispatcher.ReturnChannelAsync(rented);
        Assert.True(await Condition.WaitAsync(async () => await vhost.GetConnectionCountAsync() == 0, s_timeout));
    }

    private static RabbitMQDispatcher CreateDispatcher(VhostContext vhost)
        => new(
            NullLogger<RabbitMQDispatcher>.Instance,
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct),
            async (connection, ct) =>
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
                    cancellationToken: ct);
            });

    private static async Task AssertCanPublishAsync(RabbitMQDispatcher dispatcher, VhostContext vhost)
    {
        var channel = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        try
        {
            await channel.BasicPublishAsync("", "orders", false, new BasicProperties(), "order"u8.ToArray(),
                TestContext.Current.CancellationToken);
        }
        finally
        {
            await dispatcher.ReturnChannelAsync(channel);
        }

        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var reader = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var result = await reader.BasicGetAsync("orders", true, TestContext.Current.CancellationToken);
        Assert.Equal("order", Encoding.UTF8.GetString(result!.Body.Span));
    }

    private static async Task SendAsync(TestBus bus, ProcessPayment message)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
    }
}
