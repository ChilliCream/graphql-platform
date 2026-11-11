using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckIntegrationTests
{
    [Fact]
    public async Task HealthEndpoints_Should_KeepLivenessHealthy_When_TransportStopsAndRecovers()
    {
        // arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddMessageBus()
            .AddHealthCheck()
            .AddEventHandler<OrderHandler>()
            .AddInMemory(t => t.Endpoint("orders").Handler<OrderHandler>());
        builder.Services.AddHealthChecks().AddMessageBus();
        await using var app = builder.Build();
        app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
        app.MapHealthChecks("/health/live", new() { Predicate = check => check.Tags.Contains("live") });
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();
        var runtime = (MessagingRuntime)app.Services.GetRequiredService<IMessagingRuntime>();
        var endpoint = runtime.Transports.Single().ReceiveEndpoints.Single(e => e.Name == "orders");
        using var initial = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        // act
        await endpoint.StopAsync(runtime, TestContext.Current.CancellationToken);
        using var readiness = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        using var liveness = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        await endpoint.StartAsync(runtime, TestContext.Current.CancellationToken);
        using var recovered = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal("Healthy", await liveness.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

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
        Assert.Equal(
            "Transport 'orders' is unhealthy. The transport is not started.",
            report.Entries["MessageBus"].Description);
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
        Assert.Equal(
            "Transport 'orders' is unhealthy. Receive endpoint 'orders' is not started.",
            stopped.Entries["MessageBus"].Description);
        Assert.Equal(
            new Dictionary<string, object> { ["orders"] = "Receive endpoint 'orders' is not started." },
            stopped.Entries["MessageBus"].Data);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReportUnhealthyAfterTimeout_When_HealthRequestIsNotAnswered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddRequestHandler<UnresponsiveHealthRequestHandler>().AddInMemory();
        services.AddHealthChecks().AddMessageBus(timeout: TimeSpan.FromMilliseconds(250));
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var healthChecks = provider.GetRequiredService<HealthCheckService>();

        // act
        var report = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var entry = report.Entries["MessageBus"];
        Assert.Equal(HealthStatus.Unhealthy, entry.Status);
        Assert.Equal("Message Bus did not answer the health request within 00:00:00.2500000.", entry.Description);
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

    public sealed class UnresponsiveHealthRequestHandler : IEventRequestHandler<HealthRequest, HealthResponse>
    {
        public async ValueTask<HealthResponse> HandleAsync(HealthRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HealthResponse("OK");
        }
    }
}
