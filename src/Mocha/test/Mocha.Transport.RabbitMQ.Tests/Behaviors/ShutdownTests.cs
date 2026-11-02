using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class ShutdownTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_CancelHandlerAndRequeueMessage_When_EndpointStops()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<BlockingHandler>()
            .AddRabbitMQ(t => t.Endpoint("shutdown").Handler<BlockingHandler>())
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new TestEvent(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // act
        await runtime.StopAsync(CancellationToken.None).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var message = await channel.BasicGetAsync("shutdown", autoAck: true, TestContext.Current.CancellationToken);
        Assert.True(gate.Cancelled);
        Assert.True(message?.Redelivered);
        Assert.False(runtime.IsStarted);
    }

    public sealed class TestEvent;

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled { get; set; }
    }

    public sealed class BlockingHandler(HandlerGate gate) : IEventHandler<TestEvent>
    {
        public async ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                gate.Cancelled = true;
                throw;
            }
        }
    }
}
