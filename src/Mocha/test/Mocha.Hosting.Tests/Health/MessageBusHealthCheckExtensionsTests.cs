using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mocha.Hosting;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckExtensionsTests
{
    [Fact]
    public void AddMessageBus_Should_RegisterHealthCheck_When_Called()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddMessageBus();

        var provider = services.BuildServiceProvider();

        // Act
        var healthCheckOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        var registration = Assert.Single(healthCheckOptions.Registrations, r => r.Name == "MessageBus");

        Assert.Contains("ready", registration.Tags);
        Assert.Contains("live", registration.Tags);
    }

    [Fact]
    public void AddMessageBus_Should_ConfigureEndpoint_When_EndpointProvided()
    {
        // Arrange
        var endpoint = new Uri("rabbitmq://health-endpoint");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddMessageBus(endpoint);

        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<MessageBusHealthCheckOptions>>().Value;

        // Assert
        Assert.Equal(endpoint, options.Endpoint);
    }

    [Fact]
    public void AddMessageBus_Should_NotConfigureEndpoint_When_EndpointIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddMessageBus();

        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<MessageBusHealthCheckOptions>>().Value;

        // Assert
        Assert.Null(options.Endpoint);
    }
}
