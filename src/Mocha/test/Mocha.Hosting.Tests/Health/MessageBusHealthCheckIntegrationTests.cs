using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckIntegrationTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_TransportHasNotStarted()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddHealthCheck().AddInMemory(t => t.Name("orders"));
        services.AddHealthChecks().AddMessageBus();
        await using var provider = services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<HealthCheckService>();

        // act
        var report = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal("Transport 'orders' is unhealthy.", report.Entries["MessageBus"].Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReportUnhealthyUntilRestart_When_ApplicationEndpointIsStopped()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus()
            .AddHealthCheck()
            .AddEventHandler<OrderHandler>()
            .AddInMemory(t =>
            {
                t.Name("orders");
                t.Endpoint("orders").Handler<OrderHandler>();
            });
        services.AddHealthChecks().AddMessageBus();
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var healthChecks = provider.GetRequiredService<HealthCheckService>();
        var endpoint = runtime.Transports.Single().ReceiveEndpoints.Single(e => e.Name == "orders");
        var initial = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);

        // act
        await endpoint.StopAsync(runtime, TestContext.Current.CancellationToken);
        var stopped = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);
        await endpoint.StartAsync(runtime, TestContext.Current.CancellationToken);
        var restarted = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Healthy],
            new[] { initial.Status, stopped.Status, restarted.Status });
        Assert.Equal("Transport 'orders' is unhealthy.", stopped.Entries["MessageBus"].Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusIsRunning()
    {
        // arrange
        await using var provider = await CreateBusWithHealthCheckAsync();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // act
        var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);

        // assert
        var entry = Assert.Contains("MessageBus", report.Entries);
        Assert.Equal(HealthStatus.Healthy, entry.Status);
        Assert.Equal("Message Bus is healthy.", entry.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_CalledMultipleTimes()
    {
        // arrange
        await using var provider = await CreateBusWithHealthCheckAsync();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // act
        var statuses = new HealthStatus[3];
        for (var i = 0; i < statuses.Length; i++)
        {
            var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);
            statuses[i] = report.Entries["MessageBus"].Status;
        }

        // assert
        Assert.Equal([HealthStatus.Healthy, HealthStatus.Healthy, HealthStatus.Healthy], statuses);
    }

    private static async Task<ServiceProvider> CreateBusWithHealthCheckAsync()
    {
        var services = new ServiceCollection();
        services.AddMessageBus().AddHealthCheck().AddInMemory();
        services.AddHealthChecks().AddMessageBus();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    public sealed record OrderCreated;

    public sealed class OrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
