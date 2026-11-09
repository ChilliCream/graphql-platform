using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Hosting;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.Messaging)]
public sealed class HealthCheckTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckHealthAsync_Should_ReportUnhealthyUntilRecovery_When_LostHandlerExhaustsCapacity(
        bool loseConnection)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var services = new ServiceCollection();
        services.AddSingleton(vhost.ConnectionFactory);
        services.AddSingleton(gate);
        services.AddHealthChecks().AddMessageBus();
        await using var bus = await services.AddMessageBus()
            .AddHealthCheck()
            .AddEventHandler<StuckOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var healthChecks = bus.Provider.GetRequiredService<HealthCheckService>();
        var transport = bus.Provider.GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>().Single();

        try
        {
            var initial = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);
            await messageBus.PublishAsync(new OrderCreated { OrderId = "blocked" }, TestContext.Current.CancellationToken);
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            var busy = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);

            // act
            if (loseConnection)
            {
                await vhost.CloseConnectionOfConsumerAsync("orders");
            }
            else
            {
                await vhost.DeleteQueueAsync("orders");
            }

            await gate.Cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await vhost.WaitForNoConsumerAsync("orders", s_timeout);
            // The independent health request and reply endpoints can still process messages.
            var response = await messageBus.RequestAsync(new HealthRequest("Health Check"), TestContext.Current.CancellationToken);
            var consumerHealthyWhileBlocked = transport.ConsumerManager.IsHealthy;
            var blocked = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);
            gate.Release.TrySetResult();
            await vhost.WaitForConsumerAsync("orders", s_timeout);
            var recovered = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);
            await messageBus.PublishAsync(new OrderCreated { OrderId = "recovered" }, TestContext.Current.CancellationToken);
            var order = await gate.Recovered.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // assert
            Assert.Equal(
                [HealthStatus.Healthy, HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Healthy],
                new[] { initial.Status, busy.Status, blocked.Status, recovered.Status });
            Assert.False(consumerHealthyWhileBlocked);
            Assert.Equal($"Transport '{transport.Name}' is unhealthy.", blocked.Entries["MessageBus"].Description);
            Assert.Equal("OK", response.Message);
            Assert.Equal("recovered", order.OrderId);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<OrderCreated> Recovered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class StuckOrderHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (message.OrderId == "recovered")
            {
                gate.Recovered.TrySetResult(message);
                return;
            }

            await using var registration = cancellationToken.Register(() => gate.Cancelled.TrySetResult());
            gate.Started.TrySetResult();
            await gate.Release.Task;
        }
    }
}
