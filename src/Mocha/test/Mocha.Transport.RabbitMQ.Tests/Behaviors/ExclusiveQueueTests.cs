using System.Diagnostics;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class ExclusiveQueueTests(RabbitMQFixture fixture)
{
    private const string QueueName = "exclusive-orders";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task StartAsync_Should_ConsumeFromExclusiveQueue_When_EndpointQueueIsExclusive()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        await PublishAsync(bus, "ORD-1");
        var received = await recorder.WaitAsync(s_timeout);
        var consumers = await vhost.GetConsumerCountsAsync();

        // assert
        new { Received = received, Consumers = consumers.GetValueOrDefault(QueueName) }.MatchInlineSnapshot(
            """
            {
              "Received": true,
              "Consumers": 1
            }
            """);
    }

    [Fact]
    public async Task Consumer_Should_RedeclareExclusiveQueue_When_ConsumerConnectionClosed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);
        await PublishAsync(bus, "ORD-1");
        Assert.True(await recorder.WaitAsync(s_timeout));

        // act
        // the broker deletes an exclusive queue together with the connection that declared it
        await vhost.CloseConsumerConnectionsAsync();
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault(QueueName) == 1,
            s_timeout);
        await PublishAsync(bus, "ORD-2");
        var received = await recorder.WaitAsync(s_timeout);

        // assert
        new { Reattached = reattached, Received = received }.MatchInlineSnapshot(
            """
            {
              "Reattached": true,
              "Received": true
            }
            """);
    }

    [Fact]
    public async Task StartAsync_Should_FailFast_When_ExclusiveQueueHeldByAnotherBus()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await CreateBusAsync(vhost, recorder);

        // act
        var stopwatch = Stopwatch.StartNew();
        var services = CreateServices(vhost, new MessageRecorder()).Services.BuildServiceProvider();
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();
        var exception = await Record.ExceptionAsync(
            () => runtime.StartAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken));
        var elapsed = stopwatch.Elapsed;
        await services.DisposeAsync();

        await PublishAsync(bus, "ORD-1");
        var firstStillReceives = await recorder.WaitAsync(s_timeout);

        // assert
        new
        {
            ReplyCode = FindInterruption(exception)?.ShutdownReason?.ReplyCode,
            FailedFast = elapsed < s_timeout,
            FirstStillReceives = firstStillReceives
        }.MatchInlineSnapshot(
            """
            {
              "ReplyCode": 405,
              "FailedFast": true,
              "FirstStillReceives": true
            }
            """);
    }

    private static OperationInterruptedException? FindInterruption(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationInterruptedException interrupted)
            {
                return interrupted;
            }
        }

        return null;
    }

    private static IMessageBusHostBuilder CreateServices(VhostContext vhost, MessageRecorder recorder)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue(QueueName).Exclusive();
                t.Endpoint(QueueName).Handler<OrderCreatedHandler>();
            });

    private static Task<TestBus> CreateBusAsync(VhostContext vhost, MessageRecorder recorder)
        => CreateServices(vhost, recorder).BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new OrderCreated { OrderId = orderId }, TestContext.Current.CancellationToken);
    }
}
