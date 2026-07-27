using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class ExplicitTopologyTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ExplicitTopologyTests(RabbitMQFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
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
                t.BindExplicitly();
                t.DeclareExchange("custom-ex");
                t.DeclareQueue("custom-q");
                t.DeclareBinding("custom-ex", "custom-q");

                t.Queue("custom-q").Consumer<OrderSpyConsumer>();

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
                t.BindImplicitly();
                t.DeclareExchange("custom-ex");
                t.DeclareQueue("custom-q");
                t.DeclareBinding("custom-ex", "custom-q");

                t.Queue("custom-q").Consumer<OrderSpyConsumer>();

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
    public async Task PublishAsync_Should_RouteToQueue_When_ExplicitTopologyDeclared_UsingReceives()
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
                t.BindExplicitly();
                t.DeclareExchange("custom-ex");
                t.DeclareQueue("custom-q");
                t.DeclareBinding("custom-ex", "custom-q");

                t.Queue("custom-q")
                    .Receives<OrderCreated>();

                t.DispatchEndpoint("custom-dispatch").ToExchange("custom-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-RCV" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer on custom-q did not receive the published message");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-RCV", message.OrderId);
    }

    [Fact]
    public async Task ErrorQueue_Should_ProvisionRenamedQueue_When_FaultEndpointUsesQueueUri()
    {
        // arrange
        // The queue URI stores the name verbatim; the default convention must not kebab-case
        // or otherwise transform it. The spy endpoint on that exact name receives messages
        // forwarded there when the handler throws. Under explicit binding the publish path to the
        // handler queue is declared explicitly because the convention chain is suppressed.
        var faultCapture = new FaultCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(faultCapture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<FaultSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.BindExplicitly();
                t.DeclareExchange("orders-ex");
                t.DeclareQueue("main-ep");
                t.DeclareBinding("orders-ex", "main-ep");
                t.Queue("main-ep")
                    .Handler<ThrowingOrderHandler>()
                    .FaultEndpoint(new Uri("queue:custom-orders-error"));
                t.Queue("custom-orders-error")
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<FaultSpyConsumer>();
                t.DispatchEndpoint("orders-dispatch").ToExchange("orders-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ERROR-RENAME" }, CancellationToken.None);

        // assert
        Assert.True(await faultCapture.WaitAsync(s_timeout), "Faulted message did not arrive in the renamed error queue");
        Assert.Equal("ERROR-RENAME", Assert.Single(faultCapture.Messages).OrderId);
    }

    [Fact]
    public async Task ErrorQueue_Should_BeOmitted_When_FaultEndpointDisabled()
    {
        // arrange
        // DisableFaultEndpoint removes the error queue entirely. When the handler throws, the
        // fault middleware finds no error endpoint and silently acknowledges the message; no
        // message should arrive in any queue named after the conventional "_error" suffix. Under
        // explicit binding the publish path to the handler queue is declared explicitly because the
        // convention chain is suppressed.
        var faultCapture = new FaultCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(faultCapture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<FaultSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.BindExplicitly();
                t.DeclareExchange("orders-ex");
                t.DeclareQueue("main-ep");
                t.DeclareBinding("orders-ex", "main-ep");
                t.Queue("main-ep")
                    .Handler<ThrowingOrderHandler>()
                    .DisableFaultEndpoint();

                // Set up the spy on the conventional error queue name to prove nothing arrives there.
                t.Queue("main-ep_error")
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<FaultSpyConsumer>();
                t.DispatchEndpoint("orders-dispatch").ToExchange("orders-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ERROR-OMIT" }, CancellationToken.None);

        // assert: no message arrives in the conventional error queue because it is disabled
        Assert.False(
            await faultCapture.WaitAsync(TimeSpan.FromSeconds(3)),
            "A message arrived in the error queue even though the error queue was disabled");
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverToConsumer_When_ExplicitDestinationConfigured()
    {
        // arrange
        // AddMessage<OrderCreated> routes to an explicit exchange. The receive convention uses the
        // resolver to detect HasExplicitDestination and binds directly from that exchange into the
        // consumer queue instead of building a convention exchange chain.
        var capture = new OrderCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("custom-routing-exchange")))
            .AddConsumer<OrderSpyConsumer>()
            .AddRabbitMQ(t => t.BindImplicitly())
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-EXPLICIT-DEST" }, CancellationToken.None);

        // assert
        Assert.True(
            await capture.WaitAsync(s_timeout),
            "Consumer did not receive the message published to the explicit exchange destination");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-EXPLICIT-DEST", message.OrderId);
    }

    [Fact]
    public async Task Queue_Endpoint_DeclareQueue_Should_ProvisionOneQueue_When_SameName()
    {
        // arrange
        // Two configuration paths target the same queue name "orders":
        //   1. DeclareQueue("orders").AutoProvision(true) at transport level
        //   2. Queue("orders").Consumer<OrderSpyConsumer>() via the queue builder
        // The entity-identity merge converges both to one entity. This test
        // proves that exactly one "orders" queue is provisioned on the live broker and that
        // a message published to the dispatch exchange reaches the consumer.
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
                t.DeclareExchange("orders-ex");
                t.DeclareQueue("orders").AutoProvision(true);
                t.DeclareBinding("orders-ex", "orders");
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.DispatchEndpoint("orders-dispatch").ToExchange("orders-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        // act: list broker queues to verify exactly one "orders" queue was provisioned.
        var queues = await ListQueuesAsync(vhost.VhostName);
        _output.WriteLine($"Queues on vhost: {string.Join(", ", queues.Select(q => q.Name))}");

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "CONVERGE-THREE-WAY" }, CancellationToken.None);

        // assert: broker shows exactly one queue named "orders" (no duplicates).
        var ordersQueues = queues.Where(q => q.Name == "orders").ToList();
        Assert.Single(ordersQueues);

        // assert: message is delivered, proving the provisioned queue is operational.
        Assert.True(
            await capture.WaitAsync(s_timeout),
            "Consumer on the converged 'orders' queue did not receive the published message");

        var received = Assert.Single(capture.Messages);
        Assert.Equal("CONVERGE-THREE-WAY", received.OrderId);
    }

    private async Task<List<(string Name, string Type)>> ListQueuesAsync(string vhostName)
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "name", "type", "-p", vhostName, "--no-table-headers"]);

        Assert.NotNull(output);

        var result = new List<(string Name, string Type)>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 2 && parts[1] is not "type")
            {
                result.Add((parts[0], parts[1]));
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

    public sealed class FaultCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            Messages.Add(context.Message);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            return await _semaphore.WaitAsync(timeout);
        }
    }

    public sealed class FaultSpyConsumer(FaultCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }

    public sealed class ThrowingOrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }
}
