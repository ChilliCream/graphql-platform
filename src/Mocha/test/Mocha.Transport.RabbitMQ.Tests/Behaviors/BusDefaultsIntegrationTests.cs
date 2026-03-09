using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Xunit.Abstractions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

public class BusDefaultsIntegrationTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BusDefaultsIntegrationTests(RabbitMQFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ConfigureDefaults_Should_ProvisionQuorumQueues_When_BusStarts()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum))
            .BuildTestBusAsync();

        // act — verify the queue was created as quorum
        var queues = await ListQueuesAsync(vhost.VhostName);

        // assert — application queues should be quorum type (reply queues are
        // auto-delete and correctly remain classic since quorum doesn't support that)
        var appQueues = queues.Where(q => !q.Name.StartsWith("response-")).ToList();
        Assert.NotEmpty(appQueues);
        foreach (var (name, type) in appQueues)
        {
            Assert.Equal("quorum", type);
        }
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_QuorumQueuesUsed()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-QUORUM" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-QUORUM", order.OrderId);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_ProvisionWithCustomArguments_When_BusStarts()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConfigureDefaults(d =>
                {
                    d.Queue.QueueType = RabbitMQQueueType.Quorum;
                    d.Queue.Arguments["x-delivery-limit"] = 5;
                });
            })
            .BuildTestBusAsync();

        // act — verify queues are quorum
        var queues = await ListQueuesAsync(vhost.VhostName);

        // assert — application queues should be quorum type (reply queues are
        // auto-delete and correctly remain classic since quorum doesn't support that)
        var appQueues = queues.Where(q => !q.Name.StartsWith("response-")).ToList();
        Assert.NotEmpty(appQueues);
        foreach (var (name, type) in appQueues)
        {
            Assert.Equal("quorum", type);
        }
    }

    [Fact]
    public async Task ConfigureDefaults_Should_DeliverMessages_When_CustomDefaultsApplied()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.ConfigureDefaults(d =>
                {
                    d.Queue.QueueType = RabbitMQQueueType.Quorum;
                    d.Queue.Arguments["x-delivery-limit"] = 5;
                });
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-DEFAULTS" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the event within timeout");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-DEFAULTS", order.OrderId);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitQueue_When_QueueDeclaredWithType()
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
                t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
                t.BindHandlersExplicitly();

                // Explicitly declare a classic queue — should override the quorum default
                t.DeclareExchange("order-ex");
                t.DeclareQueue("classic-q").QueueType(RabbitMQQueueType.Classic);
                t.DeclareBinding("order-ex", "classic-q");

                t.Endpoint("classic-ep").Consumer<OrderSpyConsumer>().Queue("classic-q");
                t.DispatchEndpoint("order-dispatch").ToExchange("order-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        // act — verify queue types
        var queues = await ListQueuesAsync(vhost.VhostName);

        // assert — the classic-q should be classic, not quorum
        var classicQueue = queues.First(q => q.Name == "classic-q");
        Assert.Equal("classic", classicQueue.Type);
    }

    [Fact]
    public async Task ConfigureDefaults_Should_NotOverrideExplicitQueue_When_MessagesStillDelivered()
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
                t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
                t.BindHandlersExplicitly();

                t.DeclareExchange("order-ex");
                t.DeclareQueue("override-q").QueueType(RabbitMQQueueType.Classic);
                t.DeclareBinding("order-ex", "override-q");

                t.Endpoint("override-ep").Consumer<OrderSpyConsumer>().Queue("override-q");
                t.DispatchEndpoint("order-dispatch").ToExchange("order-ex").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-OVERRIDE" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-OVERRIDE", message.OrderId);
    }

    private async Task<List<(string Name, string Type)>> ListQueuesAsync(string vhostName)
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "name", "type", "-p", vhostName, "--no-table-headers"]);

        Assert.NotNull(output);
        _output.WriteLine($"rabbitmqctl output:\n{output}");

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
}
