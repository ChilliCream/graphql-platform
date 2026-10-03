using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryGracefulShutdownTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StopAsync_Should_WaitForInFlightHandler_When_StopIsNotCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        var gate = new HandlerGate();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<GatedOrderHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        await PublishAsync(provider, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var stoppedBeforeRelease = stop.IsCompleted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.False(stoppedBeforeRelease, "Stop should wait for the in-flight handler");
        var message = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("ORD-1", message.OrderId);
    }

    [Fact]
    public async Task StopAsync_Should_CancelInFlightHandler_When_StopTokenIsCancelled()
    {
        // arrange
        var recorder = new MessageRecorder();
        var gate = new HandlerGate();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<GatedOrderHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        await PublishAsync(provider, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // act
        await runtime.StopAsync(shutdownTimeout.Token).AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.True(gate.Cancelled.Task.IsCompleted, "The handler should observe the cancelled stop");
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public async Task StopAsync_Should_RemoveBufferedMessageFromBatch_When_StopTokenIsCancelled()
    {
        // arrange
        var recorder = new BatchMessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddBatchHandler<RecordingBatchHandler>(o =>
            {
                o.MaxBatchSize = 2;
                o.BatchTimeout = TimeSpan.FromHours(1);
            })
            .AddInMemory(t =>
            {
                t.Queue("stopping-orders").Handler<RecordingBatchHandler>();
                t.Queue("active-orders").Handler<RecordingBatchHandler>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // the message waits in the batch buffer until the batch is full
        await bus.SendAsync(
            new OrderCreated { OrderId = "ORD-BUFFERED" },
            new SendOptions { Endpoint = new Uri("queue://stopping-orders") },
            TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = runtime.Transports.Single().ReceiveEndpoints.Single(e => e.Name == "stopping-orders");
        await endpoint.StopAsync(runtime, new CancellationToken(canceled: true));

        // act
        await bus.SendAsync(
            new OrderCreated { OrderId = "ORD-1" },
            new SendOptions { Endpoint = new Uri("queue://active-orders") },
            TestContext.Current.CancellationToken);
        await bus.SendAsync(
            new OrderCreated { OrderId = "ORD-2" },
            new SendOptions { Endpoint = new Uri("queue://active-orders") },
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "The batch handler should receive a full batch");
        var batch = Assert.IsAssignableFrom<IMessageBatch<OrderCreated>>(Assert.Single(recorder.Batches));
        Assert.Equal(["ORD-1", "ORD-2"], batch.Select(m => m.OrderId).Order());
    }

    [Fact]
    public async Task StopAsync_Should_CancelBatchHandler_When_StopTokenIsCancelled()
    {
        // arrange
        var gate = new HandlerGate();
        await using var provider = await new ServiceCollection()
            .AddSingleton(gate)
            .AddMessageBus()
            .AddBatchHandler<GatedBatchHandler>(o => o.MaxBatchSize = 1)
            .AddInMemory()
            .BuildServiceProvider();

        await PublishAsync(provider, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // act
        await runtime.StopAsync(shutdownTimeout.Token).AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.True(gate.Cancelled.Task.IsCompleted, "The batch handler should observe the cancelled stop");
    }

    private static async Task PublishAsync(ServiceProvider provider, string orderId)
    {
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderCreated { OrderId = orderId }, CancellationToken.None);
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class RecordingBatchHandler(BatchMessageRecorder recorder) : IBatchEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class GatedBatchHandler(HandlerGate gate) : IBatchEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();

            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled.TrySetResult();
                throw;
            }
        }
    }

    public sealed class GatedOrderHandler(HandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();

            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled.TrySetResult();
                throw;
            }

            recorder.Record(message);
        }
    }
}
