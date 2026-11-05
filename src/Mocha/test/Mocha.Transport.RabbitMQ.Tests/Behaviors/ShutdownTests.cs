using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public sealed class ShutdownTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StopAsync_Should_LetHandlerFinishAndAck_When_StopTokenNotCancelled()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        await using var bus = await CreateBusAsync(vhost, gate);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        var stop = runtime.StopAsync(CancellationToken.None).AsTask();
        await vhost.WaitForNoConsumerAsync("shutdown", s_timeout);
        var stoppedBeforeHandlerFinished = stop.IsCompleted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var messages = await vhost.GetMessageCountAsync("shutdown");

        // assert
        Assert.False(stoppedBeforeHandlerFinished);
        Assert.True(gate.Completed);
        Assert.False(gate.Cancelled);
        Assert.Equal(0, messages);
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_CancelHandlerAndRequeueMessage_When_StopTokenCancelled()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        await using var bus = await CreateBusAsync(vhost, gate);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await runtime.StopAsync(stopTimeout.Token).AsTask()
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var message = await channel.BasicGetAsync("shutdown", autoAck: true, TestContext.Current.CancellationToken);
        Assert.False(gate.Completed);
        Assert.True(gate.Cancelled);
        Assert.True(message?.Redelivered);
        // a handler cancelled by the shutdown did not fail, so its message is not dead-lettered
        Assert.Equal(0, await vhost.GetMessageCountAsync("shutdown_error"));
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_SendReply_When_HandlerFinishesDuringStop()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddRequestHandler<GatedOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("order-status").Handler<GatedOrderStatusHandler>())
            .BuildTestBusAsync();
        await using var requester = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(_ => { })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)responder.Provider.GetRequiredService<IMessagingRuntime>();
        using var scope = requester.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);
        var request = scope.ServiceProvider.GetRequiredService<IMessageBus>().RequestAsync(
            new GetOrderStatus { OrderId = "ORD-1" },
            new SendOptions { Endpoint = new Uri("rabbitmq:q/order-status") },
            cts.Token).AsTask();
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        using var stopTimeout = new CancellationTokenSource(s_timeout);
        var stop = runtime.StopAsync(stopTimeout.Token).AsTask();
        gate.Release.TrySetResult();
        var reply = await request;
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Shipped", reply.Status);
        Assert.False(gate.Cancelled);
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_KeepReceivingReplies_When_ApplicationHandlersAreDraining()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var responder = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRabbitMQ(t => t.Endpoint("nested-responder").Handler<GetOrderStatusHandler>())
            .BuildTestBusAsync();
        var gate = new NestedRequestGate();
        await using var requester = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<NestedRequestHandler>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("nested-first").Handler<NestedRequestHandler>();
                t.Endpoint("nested-second").Handler<NestedRequestHandler>();
            })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)requester.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        using var scope = requester.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        foreach (var queue in new[] { "nested-first", "nested-second" })
        {
            await bus.SendAsync(new NestedRequest(queue),
                new SendOptions { Endpoint = new Uri($"rabbitmq:q/{queue}") },
                TestContext.Current.CancellationToken);
        }

        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        using var stopTimeout = new CancellationTokenSource(s_timeout);
        var stop = runtime.StopAsync(stopTimeout.Token).AsTask();
        bool replyWasHealthy;
        try
        {
            await vhost.WaitForNoConsumerAsync("nested-first", s_timeout);
            await vhost.WaitForNoConsumerAsync("nested-second", s_timeout);
            replyWasHealthy = ((RabbitMQReceiveEndpoint)transport.ReplyReceiveEndpoint!).IsHealthy;
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var firstMessages = await vhost.GetMessageCountAsync("nested-first");
        var secondMessages = await vhost.GetMessageCountAsync("nested-second");

        // assert
        Assert.True(replyWasHealthy);
        Assert.Equal(["nested-first", "nested-second"], gate.Responses.Order());
        Assert.Equal(0, firstMessages);
        Assert.Equal(0, secondMessages);
        Assert.False(runtime.IsStarted);
    }

    [Fact]
    public async Task StopAsync_Should_Complete_When_ConnectionClosedDuringStop()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var bus = await CreateBusAsync(vhost, gate);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await PublishAsync(bus);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        using var stopTimeout = new CancellationTokenSource(s_timeout);
        var stop = runtime.StopAsync(stopTimeout.Token).AsTask();
        await fixture.CloseAllConnectionsAsync();
        await gate.CancellationObserved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await bus.DisposeAsync();
        var closed = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() == 0,
            TimeSpan.FromSeconds(5));

        // the handler completed, but its delivery could not be acked on the closed channel
        var requeued = await vhost.GetMessageCountAsync("shutdown");

        // assert
        Assert.False(gate.Completed);
        Assert.True(gate.Cancelled);
        Assert.True(closed);
        Assert.Equal(1, requeued);
    }

    private static async Task<TestBus> CreateBusAsync(VhostContext vhost, HandlerGate gate)
        => await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<BlockingHandler>()
            .AddRabbitMQ(t => t.Endpoint("shutdown").Handler<BlockingHandler>())
            .BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TestEvent(), TestContext.Current.CancellationToken);
    }

    public sealed class TestEvent;

    public sealed record NestedRequest(string OrderId);

    public sealed class NestedRequestGate
    {
        private int _started;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentBag<string> Responses { get; } = [];

        public void HandlerStarted()
        {
            if (Interlocked.Increment(ref _started) == 2)
            {
                Started.TrySetResult();
            }
        }
    }

    public sealed class NestedRequestHandler(IMessageBus bus, NestedRequestGate gate) : IEventHandler<NestedRequest>
    {
        public async ValueTask HandleAsync(NestedRequest message, CancellationToken cancellationToken)
        {
            gate.HandlerStarted();
            await gate.Release.Task.WaitAsync(cancellationToken);
            var response = await bus.RequestAsync(
                new GetOrderStatus { OrderId = message.OrderId },
                new SendOptions { Endpoint = new Uri("rabbitmq:q/nested-responder") },
                cancellationToken);
            gate.Responses.Add(response.OrderId);
        }
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Completed { get; set; }

        public bool Cancelled { get; set; }
    }

    public sealed class GatedOrderStatusHandler(HandlerGate gate)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public async ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled = true;
                gate.CancellationObserved.TrySetResult();
                throw;
            }

            return new OrderStatusResponse { OrderId = request.OrderId, Status = "Shipped" };
        }
    }

    public sealed class BlockingHandler(HandlerGate gate) : IEventHandler<TestEvent>
    {
        public async ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
                gate.Completed = true;
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled = true;
                gate.CancellationObserved.TrySetResult();
                throw;
            }
        }
    }
}
