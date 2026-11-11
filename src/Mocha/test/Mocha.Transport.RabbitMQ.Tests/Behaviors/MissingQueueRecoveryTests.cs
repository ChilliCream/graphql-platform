using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Topology)]
public sealed class MissingQueueRecoveryTests(RabbitMQFixture fixture)
{
    private const string QueueName = "temporary-orders";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_Should_DeliverEvents_When_UpstreamExchangeBindingsAreDeletedDuringDisconnection(
        bool automaticRecovery)
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        var connectionFactory = (ConnectionFactory)vhost.ConnectionFactory;
        connectionFactory.AutomaticRecoveryEnabled = automaticRecovery;
        connectionFactory.NetworkRecoveryInterval = TimeSpan.FromSeconds(2);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("upstream")))
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("upstream");
                t.DeclareExchange("intermediate").AutoDelete();
                t.DeclareExchange("downstream").AutoDelete();
                t.DeclareBinding("upstream", "intermediate").ToExchange("intermediate");
                t.DeclareBinding("intermediate", "downstream").ToExchange("downstream");
                t.Queue(QueueName)
                    .Handler<OrderCreatedHandler>()
                    .Temporary()
                    .BindExplicitly()
                    .BindFrom(new Uri("exchange:downstream"));
            })
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-before" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        var before = await vhost.Broker.GetConsumerAsync(QueueName);

        // act
        await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
        await vhost.Broker.WaitForConsumerAsync(QueueName, otherThan: before);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-after" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        Assert.Equal(
            ["ORD-after", "ORD-before"],
            recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Order());
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverEvents_When_ConsumerConnectionIsLost()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreateEventBusAsync(vhost, recorder);
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-before" }, TestContext.Current.CancellationToken);
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        var before = await vhost.Broker.GetConsumerAsync(QueueName);

        // act
        await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
        await vhost.Broker.WaitForConsumerAsync(QueueName, otherThan: before);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-after" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        Assert.Equal(
            ["ORD-after", "ORD-before"],
            recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Order());
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverEvent_When_QueueDeclarationPermissionIsRestored()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderCreatedHandler>().Temporary())
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await using (await vhost.Faults.DenyDeclaresAsync())
        {
            await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
            await vhost.Broker.WaitForQueueDeletionAsync(QueueName);
            await vhost.Broker.WaitForNoConsumerAsync(QueueName);
        }

        await vhost.Broker.WaitForConsumerAsync(QueueName);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-recovered" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        Assert.Equal("ORD-recovered", Assert.Single(recorder.Messages.Cast<OrderCreated>()).OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverEvent_When_BindingPermissionIsRestored()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreateEventBusAsync(vhost, recorder);
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await vhost.Faults.AllowReadingQueuesOnlyAsync();

        // act
        try
        {
            await vhost.Faults.CloseConsumerConnectionAsync(QueueName);
            await vhost.Broker.WaitForQueueAsync(QueueName);
            // Queue consumption is permitted, but exchange-to-queue binding is denied.
            await vhost.Broker.WaitForNoBindingsAsync(QueueName);
        }
        finally
        {
            await vhost.Faults.SetReadPermissionAsync(".*");
        }

        await vhost.Broker.WaitForConsumerAsync(QueueName);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-bound" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(TestTimeouts.Signal));
        Assert.Equal("ORD-bound", Assert.Single(recorder.Messages.Cast<OrderCreated>()).OrderId);
    }

    [Fact]
    public async Task PublishAsync_Should_RetainEvent_When_QueueIsDeletedWhileHandlersBlockTheConsumer()
    {
        // arrange
        var gate = new HandlerGate();
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<StuckOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.PublishAsync(new OrderCreated { OrderId = "stuck" }, TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        try
        {
            // act
            // the handler ignores its cancellation, so the endpoint cannot consume again, yet its queue and
            // bindings have to exist for what is published meanwhile
            await vhost.Faults.DeleteQueueAsync("orders");
            await gate.Cancelled.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            await Wait.UntilAsync(
                () => bus.Transport.ConsumerManager.HealthFailureReason
                    == "Queue 'orders': The consumer waits for a handler of a lost channel to return.");
            await messageBus.PublishAsync(
                new OrderCreated { OrderId = "retained" },
                TestContext.Current.CancellationToken);
            var whileBlocked = await vhost.Broker.GetQueueAsync("orders");
            gate.Release.TrySetResult();
            var handled = await gate.Handled.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(new QueueInfo(Consumers: 0, Ready: 1, Unacknowledged: 0), whileBlocked);
            Assert.Equal("retained", handled.OrderId);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    private static Task<TestBus> CreateEventBusAsync(VhostContext vhost, MessageRecorder recorder)
    {
        ((ConnectionFactory)vhost.ConnectionFactory).NetworkRecoveryInterval = TimeSpan.FromSeconds(2);

        return new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderCreatedHandler>().Temporary())
            .BuildTestBusAsync();
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<OrderCreated> Handled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class StuckOrderHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (message.OrderId != "stuck")
            {
                gate.Handled.TrySetResult(message);
                return;
            }

            await using var registration = cancellationToken.Register(() => gate.Cancelled.TrySetResult());
            gate.Started.TrySetResult();
            await gate.Release.Task;
        }
    }
}
