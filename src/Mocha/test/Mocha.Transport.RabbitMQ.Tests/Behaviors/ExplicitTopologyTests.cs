using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class ExplicitTopologyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public ExplicitTopologyTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToQueue_When_ExplicitTopologyDeclared()
    {
        // arrange
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
                t.DeclareExchange("custom-ex");
                t.DeclareQueue("custom-q");
                t.DeclareBinding("custom-ex", "custom-q");

                t.Endpoint("custom-ep").Consumer<OrderSpyConsumer>().Queue("custom-q");

                t.DispatchEndpoint("custom-dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-TOPO" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer on custom-q did not receive the published message");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-TOPO", message.OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToQueue_When_ExplicitTopologyDeclared_WithImplicit()
    {
        // arrange
        var capture = new OrderCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.BindHandlersImplicitly();
                t.DeclareExchange("custom-ex");
                t.DeclareQueue("custom-q");
                t.DeclareBinding("custom-ex", "custom-q");

                t.Endpoint("custom-ep").Consumer<OrderSpyConsumer>().Queue("custom-q");

                t.DispatchEndpoint("custom-dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-TOPO" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer on custom-q did not receive the published message");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-TOPO", message.OrderId);
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
