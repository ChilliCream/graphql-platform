using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Events;
using Moq;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_BusRespondsOK()
    {
        // arrange
        var bus = CreateBus(new HealthResponse("OK"));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime());

        // act
        var result = await healthCheck.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Message Bus is healthy.", result.Description);
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task CheckHealthAsync_Should_ReturnFailureStatus_When_BusRespondsNonOK(HealthStatus failureStatus)
    {
        // arrange
        var bus = CreateBus(new HealthResponse("ERROR"));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime());

        // act
        var result = await healthCheck.CheckHealthAsync(CreateContext(failureStatus), CancellationToken.None);

        // assert
        Assert.Equal(failureStatus, result.Status);
        Assert.Equal("Message Bus is unhealthy.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_RouteToEndpoint_When_EndpointConfigured()
    {
        // arrange
        var endpoint = new Uri("rabbitmq://health-endpoint");
        var bus = CreateBus(new HealthResponse("OK"));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime(), endpoint);

        // act
        await healthCheck.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // assert
        bus.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == endpoint),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_UseDefaultRouting_When_NoEndpointConfigured()
    {
        // arrange
        var bus = CreateBus(new HealthResponse("OK"));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime());

        // act
        await healthCheck.CheckHealthAsync(CreateContext(), CancellationToken.None);

        // assert
        bus.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == null),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ExpireHealthRequestAfterTimeout_When_RequestIsSent()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        SendOptions? sendOptions = null;
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEventRequest<HealthResponse> _, SendOptions options, CancellationToken _) =>
            {
                sendOptions = options;
                return new ValueTask<HealthResponse>(new HealthResponse("OK"));
            });
        var healthCheck = new MessageBusHealthCheck(
            bus.Object,
            CreateRuntime(),
            new FixedTimeProvider(now),
            new MessageBusHealthCheckOptions { Timeout = TimeSpan.FromSeconds(7) });

        // act
        await healthCheck.CheckHealthAsync(CreateContext(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(now.AddSeconds(7), sendOptions?.ExpirationTime);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnFailureStatus_When_BusDoesNotAnswerWithinTimeout()
    {
        // arrange
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEventRequest<HealthResponse> _, SendOptions _, CancellationToken cancellationToken)
                => new ValueTask<HealthResponse>(WaitForCancellationAsync(cancellationToken)));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime(), timeout: TimeSpan.FromMilliseconds(100));

        // act
        var result = await healthCheck
            .CheckHealthAsync(CreateContext(HealthStatus.Degraded), TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Message Bus did not answer the health request within 00:00:00.1000000.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnFailureStatus_When_ResponseTimesOut()
    {
        // arrange
        var failure = new ResponseTimeoutException("correlation-1", TimeSpan.FromMinutes(2));
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException<HealthResponse>(failure));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime());

        // act
        var result = await healthCheck.CheckHealthAsync(
            CreateContext(HealthStatus.Degraded),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Message Bus did not answer the health request within 00:02:00.", result.Description);
        Assert.Same(failure, result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_PropagateCancellation_When_CallerCancels()
    {
        // arrange
        using var caller = new CancellationTokenSource();
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEventRequest<HealthResponse> _, SendOptions _, CancellationToken cancellationToken) =>
            {
                caller.Cancel();
                return new ValueTask<HealthResponse>(WaitForCancellationAsync(cancellationToken));
            });
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime());

        // act
        var exception = await Record.ExceptionAsync(
            () => healthCheck.CheckHealthAsync(CreateContext(), caller.Token));

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_TransportFailsDuringRequest()
    {
        // arrange
        var transport = CreateTransport("orders", healthFailureReason: null);
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                transport.SetupGet(t => t.HealthFailureReason).Returns("The dispatcher connection is unavailable.");
                return new ValueTask<HealthResponse>(new HealthResponse("OK"));
            });
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime(transport.Object));

        // act
        var result = await healthCheck.CheckHealthAsync(CreateContext(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(
            "Transport 'orders' is unhealthy. The dispatcher connection is unavailable.",
            result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReportEveryUnhealthyTransport_When_SeveralTransportsAreUnhealthy()
    {
        // arrange
        var orders = CreateTransport("orders", "The dispatcher connection is unavailable.");
        var payments = CreateTransport("payments", healthFailureReason: null);
        var shipping = CreateTransport("shipping", "Receive endpoint 'parcels' is not started.");
        var bus = CreateBus(new HealthResponse("OK"));
        var healthCheck = CreateHealthCheck(
            bus.Object,
            CreateRuntime(orders.Object, payments.Object, shipping.Object));

        // act
        var result = await healthCheck.CheckHealthAsync(
            CreateContext(HealthStatus.Degraded),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal(
            "Transport 'orders' is unhealthy. The dispatcher connection is unavailable.",
            result.Description);
        Assert.Equal(
            new Dictionary<string, object>
            {
                ["orders"] = "The dispatcher connection is unavailable.",
                ["shipping"] = "Receive endpoint 'parcels' is not started."
            },
            result.Data);
        bus.VerifyNoOtherCalls();
    }

    private static MessageBusHealthCheck CreateHealthCheck(
        IMessageBus bus,
        IMessagingRuntime runtime,
        Uri? endpoint = null,
        TimeSpan? timeout = null)
        => new(
            bus,
            runtime,
            TimeProvider.System,
            new MessageBusHealthCheckOptions { Endpoint = endpoint, Timeout = timeout ?? s_timeout });

    private static HealthCheckContext CreateContext(HealthStatus failureStatus = HealthStatus.Unhealthy)
        => new()
        {
            Registration = new HealthCheckRegistration("MessageBus", Mock.Of<IHealthCheck>(), failureStatus, tags: null)
        };

    private static Mock<IMessageBus> CreateBus(HealthResponse response)
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<HealthResponse>(response));
        return bus;
    }

    private static Mock<MessagingTransport> CreateTransport(string name, string? healthFailureReason)
    {
        var transport = new Mock<MessagingTransport>();
        transport.Object.Name = name;
        transport.SetupGet(t => t.HealthFailureReason).Returns(healthFailureReason);
        return transport;
    }

    private static IMessagingRuntime CreateRuntime(params MessagingTransport[] transports)
    {
        var runtime = new Mock<IMessagingRuntime>();
        runtime.SetupGet(r => r.Transports).Returns([.. transports]);
        return runtime.Object;
    }

    private static async Task<HealthResponse> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return new HealthResponse("OK");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
