using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

/// <summary>
/// RabbitMQ does NOT have native scheduling and ships no fallback scheduling store.
/// Scheduled dispatch through this transport now fails fast with NotSupportedException
/// instead of being silently delivered immediately. To enable scheduling for RabbitMQ
/// register a fallback scheduling store (e.g. UsePostgresScheduling()).
/// </summary>
[Collection("RabbitMQ")]
public class SchedulingTests
{
    private readonly RabbitMQFixture _fixture;

    public SchedulingTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_Throw_When_ScheduledTimeSetAndNoStore()
    {
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => messageBus.PublishAsync(
                new OrderCreated { OrderId = "ORD-SCHED-1" },
                new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(30) },
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_ScheduledTimeSetAndNoStore()
    {
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => messageBus.SendAsync(
                new ProcessPayment { OrderId = "ORD-SCHED-2", Amount = 99.99m },
                new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(30) },
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task PublishAsync_Should_Throw_When_ScheduledTimeInPastAndNoStore()
    {
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => messageBus.PublishAsync(
                new OrderCreated { OrderId = "ORD-PAST-1" },
                new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1) },
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_ScheduledTimeInPastAndNoStore()
    {
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => messageBus.SendAsync(
                new ProcessPayment { OrderId = "ORD-PAST-2", Amount = 25.00m },
                new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1) },
                CancellationToken.None).AsTask());
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
