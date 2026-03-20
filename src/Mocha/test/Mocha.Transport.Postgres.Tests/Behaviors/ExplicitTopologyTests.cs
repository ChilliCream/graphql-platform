using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class ExplicitTopologyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly PostgresFixture _fixture;

    public ExplicitTopologyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToQueue_When_ExplicitTopologyDeclared()
    {
        // arrange
        var capture = new OrderCapture();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.BindHandlersExplicitly();
                t.DeclareTopic("custom-topic");
                t.DeclareQueue("custom-q");
                t.DeclareSubscription("custom-topic", "custom-q");

                t.Endpoint("custom-ep").Consumer<OrderSpyConsumer>().Queue("custom-q");

                t.DispatchEndpoint("custom-dispatch").ToTopic("custom-topic").Publish<OrderCreated>();
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<OrderSpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);
                t.BindHandlersImplicitly();
                t.DeclareTopic("custom-topic");
                t.DeclareQueue("custom-q");
                t.DeclareSubscription("custom-topic", "custom-q");

                t.Endpoint("custom-ep").Consumer<OrderSpyConsumer>().Queue("custom-q");

                t.DispatchEndpoint("custom-dispatch").ToTopic("custom-topic").Publish<OrderCreated>();
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
