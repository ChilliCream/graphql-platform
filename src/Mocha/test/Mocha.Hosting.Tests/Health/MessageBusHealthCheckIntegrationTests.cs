using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Hosting;
using Mocha.Transport.InMemory;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckIntegrationTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusIsRunning()
    {
        // Arrange
        await using var provider = await CreateBusWithHealthCheckAsync();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // Act
        var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);

        // Assert
        var entry = Assert.Contains("MessageBus", report.Entries);
        Assert.Equal(HealthStatus.Healthy, entry.Status);
        Assert.Equal("Message Bus is healthy.", entry.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_CalledMultipleTimes()
    {
        // Arrange
        await using var provider = await CreateBusWithHealthCheckAsync();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        // Act & Assert
        for (var i = 0; i < 3; i++)
        {
            var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);
            var entry = Assert.Contains("MessageBus", report.Entries);
            Assert.Equal(HealthStatus.Healthy, entry.Status);
        }
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
}
