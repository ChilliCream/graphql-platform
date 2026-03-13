using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusRespondsOK()
    {
        // Arrange
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options);

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
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("ERROR")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options);

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
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions { Endpoint = endpoint });
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        busMock.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == endpoint),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_UseDefaultSendOptions_When_NoEndpointConfigured()
    {
        // Arrange
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options);

        // Act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        busMock.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == null),
            It.IsAny<CancellationToken>()), Times.Once());
    }
}
