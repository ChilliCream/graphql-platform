using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// End-to-end Docker-gated tests for the unified <c>t.Queue(name, q => ...)</c> front door.
/// Verifies that consumer placement via the unified handle routes messages to the consumer.
/// </summary>
[Collection("RabbitMQ")]
public class UnifiedQueueBehaviorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public UnifiedQueueBehaviorTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToConsumer_When_PlacedViaUnifiedQueue()
    {
        // arrange
        // The unified Queue() front door places a consumer on the "orders" queue. Publishing
        // an OrderCreated message should deliver to that consumer through the convention exchange
        // chain that binds into the queue automatically (AutoBind defaults to on).
        var capture = new OrderCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "UNIFIED-ROUTE" }, CancellationToken.None);

        // assert
        Assert.True(
            await capture.WaitAsync(s_timeout),
            "Consumer placed via the unified Queue() front door did not receive the published message");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("UNIFIED-ROUTE", message.OrderId);
    }

    public sealed class OrderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            Messages.Add(context.Message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class OrderSpyConsumer(OrderCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }
}
