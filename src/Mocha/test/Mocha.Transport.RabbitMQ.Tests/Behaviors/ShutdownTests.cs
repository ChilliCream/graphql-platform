using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
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
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        var stoppedBeforeHandlerFinished = stop.IsCompleted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var messages = await vhost.GetMessageCountAsync("shutdown");

        // assert
        new
        {
            StoppedBeforeHandlerFinished = stoppedBeforeHandlerFinished,
            gate.Completed,
            gate.Cancelled,
            Messages = messages,
            runtime.IsStarted
        }.MatchInlineSnapshot(
            """
            {
              "StoppedBeforeHandlerFinished": false,
              "Completed": true,
              "Cancelled": false,
              "Messages": 0,
              "IsStarted": false
            }
            """);
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
        // a handler cancelled by the shutdown did not fail, so its message is not dead-lettered
        new
        {
            gate.Completed,
            gate.Cancelled,
            Redelivered = message?.Redelivered,
            DeadLettered = await vhost.GetMessageCountAsync("shutdown_error"),
            runtime.IsStarted
        }.MatchInlineSnapshot(
            """
            {
              "Completed": false,
              "Cancelled": true,
              "Redelivered": true,
              "DeadLettered": 0,
              "IsStarted": false
            }
            """);
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
        new { reply.Status, gate.Cancelled, runtime.IsStarted }.MatchInlineSnapshot(
            """
            {
              "Status": "Shipped",
              "Cancelled": false,
              "IsStarted": false
            }
            """);
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
        await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        gate.Release.TrySetResult();
        var stopped = await Task.WhenAny(stop, Task.Delay(s_timeout, TestContext.Current.CancellationToken)) == stop;
        await bus.DisposeAsync();
        var closed = await Condition.WaitAsync(
            async () => await vhost.GetConnectionCountAsync() == 0,
            TimeSpan.FromSeconds(5));

        // the handler completed, but its delivery could not be acked on the closed channel
        var requeued = await vhost.GetMessageCountAsync("shutdown");

        // assert
        new { Stopped = stopped, gate.Completed, Closed = closed, Requeued = requeued }.MatchInlineSnapshot(
            """
            {
              "Stopped": true,
              "Completed": true,
              "Closed": true,
              "Requeued": 1
            }
            """);
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

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
                throw;
            }
        }
    }
}
