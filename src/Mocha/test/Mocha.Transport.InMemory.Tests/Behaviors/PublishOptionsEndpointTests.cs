using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class PublishOptionsEndpointTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// When <see cref="PublishOptions.Endpoint"/> is set, the message is dispatched directly to
    /// the addressed endpoint rather than routed through the convention publish topic. The handler
    /// on that endpoint receives the message even though the URI addresses the queue rather than
    /// the convention topic.
    /// </summary>
    [Fact]
    public async Task PublishAsync_Should_UseEndpointOverride_When_PublishOptionsEndpointSet()
    {
        // arrange
        // One handler claimed explicitly on a named queue. A dispatch endpoint for that queue
        // is declared so the bus can resolve queue://order-target as a known dispatch address.
        var recorder = new MessageRecorder();

        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                // Bind the handler explicitly to "order-target" queue.
                t.Endpoint("order-target-ep")
                    .Handler<OrderCreatedHandler>()
                    .Queue("order-target");

                // Declare a dispatch endpoint so the bus resolves queue://order-target
                // without a topic hop.
                t.DispatchEndpoint("order-target-dispatch")
                    .ToQueue("order-target")
                    .Publish<OrderCreated>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish with Endpoint override addresses the queue directly, bypassing the
        // convention topic; the bus dispatches to the named endpoint without topic fan-out.
        await bus.PublishAsync(
            new OrderCreated { OrderId = "ORD-ENDPOINT" },
            new PublishOptions { Endpoint = new Uri("queue://order-target") },
            CancellationToken.None);

        // assert - handler on the targeted queue receives the message
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Handler did not receive the message via PublishOptions.Endpoint override");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-ENDPOINT", order.OrderId);
    }
}
