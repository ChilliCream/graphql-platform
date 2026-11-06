using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class MissingQueueRecoveryTests(RabbitMQFixture fixture)
{
    private const string QueueName = "temporary-orders";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

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
        Assert.True(await recorder.WaitAsync(s_timeout));

        // act
        await vhost.CloseConsumerConnectionAsync(QueueName);
        await vhost.WaitForQueueDeletionAsync(QueueName, s_timeout);
        await vhost.WaitForConsumerAsync(QueueName, TimeSpan.FromSeconds(60));
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-after" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
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
        Assert.True(await recorder.WaitAsync(s_timeout));

        // act
        await vhost.CloseConsumerConnectionAsync(QueueName);
        await vhost.WaitForQueueDeletionAsync(QueueName, s_timeout);
        await vhost.WaitForConsumerAsync(QueueName, s_timeout);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-after" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
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
        await using var bus = await CreateEventBusAsync(vhost, recorder);
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await vhost.SetConfigurePermissionAsync("^$");

        // act
        try
        {
            await vhost.CloseConsumerConnectionAsync(QueueName);
            await vhost.WaitForQueueDeletionAsync(QueueName, s_timeout);
            // Keep declarations denied across several recovery attempts.
            await Task.Delay(TimeSpan.FromSeconds(7), TestContext.Current.CancellationToken);
        }
        finally
        {
            await vhost.SetConfigurePermissionAsync(".*");
        }

        await vhost.WaitForConsumerAsync(QueueName, s_timeout);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-recovered" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
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
        await vhost.AllowReadingQueuesOnlyAsync();

        // act
        try
        {
            await vhost.CloseConsumerConnectionAsync(QueueName);
            await vhost.WaitForQueueDeletionAsync(QueueName, s_timeout);
            await vhost.WaitForQueueAsync(QueueName, s_timeout);
            // Queue consumption is permitted, but exchange-to-queue binding is denied.
            await vhost.WaitForNoBindingsAsync(QueueName, s_timeout);
        }
        finally
        {
            await vhost.SetReadPermissionAsync(".*");
        }

        await vhost.WaitForConsumerAsync(QueueName, s_timeout);
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-bound" }, TestContext.Current.CancellationToken);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout));
        Assert.Equal("ORD-bound", Assert.Single(recorder.Messages.Cast<OrderCreated>()).OrderId);
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
}
