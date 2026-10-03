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
