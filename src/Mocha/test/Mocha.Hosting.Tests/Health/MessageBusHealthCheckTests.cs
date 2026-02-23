using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mocha.Hosting;
using NSubstitute;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusRespondsOK()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        bus.RequestAsync(Arg.Any<HealthRequest>(), Arg.Any<SendOptions>(), Arg.Any<CancellationToken>())
            .Returns(new HealthResponse("OK"));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(bus, options);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Message Bus is healthy.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_BusRespondsNonOK()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        bus.RequestAsync(Arg.Any<HealthRequest>(), Arg.Any<SendOptions>(), Arg.Any<CancellationToken>())
            .Returns(new HealthResponse("ERROR"));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(bus, options);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Message Bus is unhealthy.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_RouteToEndpoint_When_EndpointConfigured()
    {
        // Arrange
        var endpoint = new Uri("rabbitmq://health-endpoint");
        var bus = Substitute.For<IMessageBus>();
        bus.RequestAsync(Arg.Any<HealthRequest>(), Arg.Any<SendOptions>(), Arg.Any<CancellationToken>())
            .Returns(new HealthResponse("OK"));

        var options = Options.Create(new MessageBusHealthCheckOptions { Endpoint = endpoint });
        var healthCheck = new MessageBusHealthCheck(bus, options);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        await bus.Received(1)
            .RequestAsync(
                Arg.Any<HealthRequest>(),
                Arg.Is<SendOptions>(o => o.Endpoint == endpoint),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_UseDefaultSendOptions_When_NoEndpointConfigured()
    {
        // Arrange
        var bus = Substitute.For<IMessageBus>();
        bus.RequestAsync(Arg.Any<HealthRequest>(), Arg.Any<SendOptions>(), Arg.Any<CancellationToken>())
            .Returns(new HealthResponse("OK"));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(bus, options);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        await bus.Received(1)
            .RequestAsync(
                Arg.Any<HealthRequest>(),
                Arg.Is<SendOptions>(o => o.Endpoint == null),
                Arg.Any<CancellationToken>());
    }
}
