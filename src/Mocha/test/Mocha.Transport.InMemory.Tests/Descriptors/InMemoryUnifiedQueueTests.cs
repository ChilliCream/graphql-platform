using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Descriptors;

/// <summary>
/// Verifies the delivery behavior and endpoint materialization of the unified
/// <c>t.Queue(name)</c> API on the in-memory transport.
/// </summary>
public class InMemoryUnifiedQueueTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Queue_Should_DeliverMessages_When_ConsumerAttachedViaUnifiedQueue()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddInMemory(t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(
            new OrderCreated { OrderId = "q-unified-queue-01" },
            new SendOptions { Endpoint = new Uri("queue://orders") },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Consumer attached via the unified Queue() API should receive the message.");
        var msg = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("q-unified-queue-01", msg.OrderId);
    }

    [Fact]
    public void Queue_Should_MaterializeReceiveEndpoint_When_NoConsumersOrReceives()
    {
        // arrange
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("dispatch-target");
            });
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .FirstOrDefault(e => e.Queue.Name == "dispatch-target");

        // assert
        Assert.NotNull(endpoint);
        Assert.Contains(topology.Queues, q => q.Name == "dispatch-target");
    }

    public sealed class OrderSpyConsumer(MessageRecorder recorder) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }
}
