using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusRespondsOK()
    {
        // arrange
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options, CreateRuntime());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Message Bus is healthy.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_BusRespondsNonOK()
    {
        // arrange
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("ERROR")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options, CreateRuntime());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Message Bus is unhealthy.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_RouteToEndpoint_When_EndpointConfigured()
    {
        // arrange
        var endpoint = new Uri("rabbitmq://health-endpoint");
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions { Endpoint = endpoint });
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options, CreateRuntime());

        // act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        busMock.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == endpoint),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_UseDefaultSendOptions_When_NoEndpointConfigured()
    {
        // arrange
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(new HealthResponse("OK")));

        var options = Options.Create(new MessageBusHealthCheckOptions());
        var healthCheck = new MessageBusHealthCheck(busMock.Object, options, CreateRuntime());

        // act
        await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        busMock.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == null),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_TransportFailsDuringRequest()
    {
        // arrange
        var transport = new Mock<MessagingTransport>();
        transport.Object.Name = "orders";
        transport.SetupGet(t => t.IsHealthy).Returns(true);
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(), It.IsAny<SendOptions>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                transport.SetupGet(t => t.IsHealthy).Returns(false);
                return new ValueTask<HealthResponse>(new HealthResponse("OK"));
            });
        var healthCheck = new MessageBusHealthCheck(
            bus.Object,
            Options.Create(new MessageBusHealthCheckOptions()),
            CreateRuntime(transport.Object));

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Transport 'orders' is unhealthy.", result.Description);
    }

    private static IMessagingRuntime CreateRuntime(params MessagingTransport[] transports)
    {
        var runtime = new Mock<IMessagingRuntime>();
        runtime.SetupGet(r => r.Transports).Returns([.. transports]);
        return runtime.Object;
    }
}
