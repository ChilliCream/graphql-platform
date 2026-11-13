using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            new MessageBusHealthCheckOptions());

        // act
        await healthCheck.CheckHealthAsync(
            CreateContext(timeout: TimeSpan.FromSeconds(7)),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(now.AddSeconds(7), sendOptions?.ExpirationTime);
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
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_CheckFailsDuringRequest()
    {
        // arrange
        var check = new StubHealthCheck("orders", reason: null);
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                check.Reason = "The dispatcher connection is unavailable.";
                return new ValueTask<HealthResponse>(new HealthResponse("OK"));
            });
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime(check));

        // act
        var result = await healthCheck.CheckHealthAsync(CreateContext(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("The dispatcher connection is unavailable.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReportEveryFailure_When_SeveralChecksFail()
    {
        // arrange
        var orders = new StubHealthCheck("orders", "The dispatcher connection is unavailable.");
        var payments = new StubHealthCheck("payments", reason: null);
        var shipping = new StubHealthCheck("shipping", "Receive endpoint 'parcels' is not started.");
        var bus = CreateBus(new HealthResponse("OK"));
        var healthCheck = CreateHealthCheck(bus.Object, CreateRuntime(orders, payments, shipping));

        // act
        var result = await healthCheck.CheckHealthAsync(
            CreateContext(HealthStatus.Degraded),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("The dispatcher connection is unavailable.", result.Description);
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
        Uri? endpoint = null)
        => new(
            bus,
            runtime,
            TimeProvider.System,
            new MessageBusHealthCheckOptions { Endpoint = endpoint });

    private static HealthCheckContext CreateContext(
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        TimeSpan? timeout = null)
        => new()
        {
            Registration = new HealthCheckRegistration(
                "MessageBus",
                Mock.Of<IHealthCheck>(),
                failureStatus,
                tags: null,
                timeout ?? s_timeout)
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

    private static IMessagingRuntime CreateRuntime(params IMessageBusHealthCheck[] checks)
    {
        var services = new ServiceCollection();

        foreach (var check in checks)
        {
            services.AddSingleton(check);
        }

        var runtime = new Mock<IMessagingRuntime>();
        runtime.SetupGet(r => r.Services).Returns(services.BuildServiceProvider());
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

    private sealed class StubHealthCheck(string name, string? reason) : IMessageBusHealthCheck
    {
        public string? Reason { get; set; } = reason;

        public ValueTask CheckAsync(MessageBusHealthContext context, CancellationToken cancellationToken)
        {
            if (Reason is not null)
            {
                context.ReportUnhealthy(name, Reason);
            }

            return ValueTask.CompletedTask;
        }
    }
}
