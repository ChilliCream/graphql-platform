using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests;

public class EventHubHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_ProcessorNotStarted()
    {
        // arrange - endpoint configured but processor never started
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var healthCheck = new EventHubHealthCheck(transport);

        var receiveEndpoints = transport.ReceiveEndpoints
            .OfType<EventHubReceiveEndpoint>()
            .ToList();
        Assert.NotEmpty(receiveEndpoints);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("No Event Hub processors are running", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_IncludeStoppedEndpointData_When_ProcessorNotStarted()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var healthCheck = new EventHubHealthCheck(transport);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // assert
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data["running"]);
        Assert.True((int)result.Data["stopped"] > 0);
    }

    [Fact]
    public void IsProcessorRunning_Should_ReturnFalse_When_ProcessorNotStarted()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<EventHubReceiveEndpoint>()
            .First();

        // act & assert
        Assert.False(endpoint.IsProcessorRunning);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddEventHub(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .BuildRuntime();
    }
}
