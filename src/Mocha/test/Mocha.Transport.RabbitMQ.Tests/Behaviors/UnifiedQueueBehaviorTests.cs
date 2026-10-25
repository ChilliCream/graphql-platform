using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// End-to-end Docker-gated tests for the unified <c>t.Queue(name)</c> API.
/// Verifies that consumer placement via the Queue() API routes messages to the consumer.
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
        // The unified Queue() API declares the "orders" queue and places a consumer on it. Under
        // explicit binding the convention publish chain is suppressed, so the publish is routed to
        // that queue via the message declaration; publishing OrderCreated must reach the consumer.
        var capture = new OrderCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQQueue("orders")))
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
            "Consumer placed via the unified Queue() API did not receive the published message");

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
