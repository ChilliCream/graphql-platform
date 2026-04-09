using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// RabbitMQ does NOT have native scheduling. Without an external scheduler configured,
/// messages with ScheduledTime are delivered immediately. These tests verify that
/// ScheduledTime does not break the pipeline and messages are still delivered.
/// </summary>
[Collection("RabbitMQ")]
public class SchedulingTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public SchedulingTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverMessage_When_ScheduledTimeIsInFuture()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(30);

        // act - RabbitMQ delivers immediately regardless of ScheduledTime
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-SCHED-1" },
            new PublishOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        // assert - message should be delivered (not lost)
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message with ScheduledTime should still be delivered via RabbitMQ");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-SCHED-1", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverMessage_When_ScheduledTimeIsInFuture()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var scheduledTime = DateTimeOffset.UtcNow.AddSeconds(30);

        // act - RabbitMQ delivers immediately regardless of ScheduledTime
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-SCHED-2", Amount = 99.99m },
            new SendOptions { ScheduledTime = scheduledTime },
            CancellationToken.None);

        // assert - message should be delivered (not lost)
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message with ScheduledTime should still be delivered via RabbitMQ");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-SCHED-2", payment.OrderId);
        Assert.Equal(99.99m, payment.Amount);
    }

    [Fact]
    public async Task PublishAsync_Should_DeliverMessage_When_ScheduledTimeIsInPast()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        // act
        await messageBus.PublishAsync(
            new OrderCreated { OrderId = "ORD-PAST-1" },
            new PublishOptions { ScheduledTime = pastTime },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered when ScheduledTime is in the past");

        var message = Assert.Single(recorder.Messages);
        var order = Assert.IsType<OrderCreated>(message);
        Assert.Equal("ORD-PAST-1", order.OrderId);
    }

    [Fact]
    public async Task SendAsync_Should_DeliverMessage_When_ScheduledTimeIsInPast()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-1);

        // act
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "ORD-PAST-2", Amount = 25.00m },
            new SendOptions { ScheduledTime = pastTime },
            CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "Message should be delivered when ScheduledTime is in the past");

        var message = Assert.Single(recorder.Messages);
        var payment = Assert.IsType<ProcessPayment>(message);
        Assert.Equal("ORD-PAST-2", payment.OrderId);
        Assert.Equal(25.00m, payment.Amount);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
