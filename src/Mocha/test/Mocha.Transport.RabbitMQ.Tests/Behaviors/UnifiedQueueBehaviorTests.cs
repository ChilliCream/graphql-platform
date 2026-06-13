using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// End-to-end Docker-gated tests for the unified <c>t.Queue(name, q => ...)</c> front door.
/// Verifies that consumer placement via the unified handle routes messages to the consumer,
/// and that an entity-only queue with a <c>BindFrom</c> binding accumulates messages on the broker
/// without a consuming endpoint draining them.
/// </summary>
[Collection("RabbitMQ")]
public class UnifiedQueueBehaviorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;
    private readonly ITestOutputHelper _output;

    public UnifiedQueueBehaviorTests(RabbitMQFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
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
                t.BindHandlersExplicitly();
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

    [Fact]
    public async Task EntityOnlyQueue_Should_AccumulateMessages_When_BindFromDeclared()
    {
        // arrange
        // An entity-only Queue() handle (no consumer, no Receives) with a BindFrom binding from
        // "audit-events" receives messages published to that exchange but has no consumer draining
        // them. After publishing one message the broker queue must contain exactly one message.
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("audit-events")))
            .AddRabbitMQ(t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("audit", q => q.BindFrom(new Uri("exchange:audit-events")));
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "AUDIT-ACC" }, TestContext.Current.CancellationToken);

        // allow broker propagation before checking queue depth
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // assert: list queue depths and find the "audit" queue with exactly one message
        var queues = await ListQueuesWithDepthAsync(vhost.VhostName);
        _output.WriteLine($"Queue depths: {string.Join(", ", queues.Select(q => $"{q.Name}={q.Messages}"))}");

        var auditQueueMessages = queues
            .Where(q => q.Name == "audit")
            .Select(q => q.Messages)
            .SingleOrDefault(-1);
        Assert.Equal(1, auditQueueMessages);
    }

    private async Task<List<(string Name, int Messages)>> ListQueuesWithDepthAsync(string vhostName)
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "name", "messages", "-p", vhostName, "--no-table-headers"]);

        Assert.NotNull(output);

        var result = new List<(string Name, int Messages)>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var count))
            {
                result.Add((parts[0], count));
            }
        }

        return result;
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
