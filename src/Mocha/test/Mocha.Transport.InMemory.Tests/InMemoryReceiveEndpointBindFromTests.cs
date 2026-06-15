using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

/// <summary>
/// Verifies that BindFrom intents on the in-memory transport reject routing keys at topology
/// discovery time, since the in-memory transport does not support routing key semantics.
/// </summary>
public class InMemoryReceiveEndpointBindFromTests
{
    [Fact]
    public void BindFrom_Should_FailBuild_When_InMemoryRoutingKeyNonNull()
    {
        // arrange
        // The in-memory transport does not support routing keys; a BindFrom with a non-null routing
        // key must fail at topology discovery time with a targeted build error.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CreateRuntime(
                b => b.AddConsumer<OrderSpyConsumer>(),
                t =>
                {
                    t.BindExplicitly();
                    t.Queue("orders")
                        .Consumer<OrderSpyConsumer>()
                        .BindFrom(new Uri("topic:source-topic"), routingKey: "key.not.valid");
                }));

        // assert
        Assert.Contains("in-memory", ex.Message);
        Assert.Contains("routing key", ex.Message);
        Assert.Contains("orders", ex.Message);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddInMemory(configureTransport)
            .BuildRuntime();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
