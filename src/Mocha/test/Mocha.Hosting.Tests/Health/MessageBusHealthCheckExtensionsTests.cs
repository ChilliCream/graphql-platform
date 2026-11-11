using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusHealthCheckExtensionsTests
{
    [Fact]
    public void AddMessageBus_Should_RegisterReadinessCheck_When_CalledWithDefaults()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddMessageBus();
        using var provider = services.BuildServiceProvider();

        // act
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        var registration = Assert.Single(registrations);
        Assert.Equal("MessageBus", registration.Name);
        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Equal(["ready"], registration.Tags);
    }

    [Fact]
    public void AddMessageBus_Should_RegisterCheckWithNameFailureStatusAndTags_When_Provided()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMessageBus(name: "orders-bus", failureStatus: HealthStatus.Degraded, tags: ["live"]);
        using var provider = services.BuildServiceProvider();

        // act
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        var registration = Assert.Single(registrations);
        Assert.Equal("orders-bus", registration.Name);
        Assert.Equal(HealthStatus.Degraded, registration.FailureStatus);
        Assert.Equal(["live"], registration.Tags);
    }

    [Fact]
    public async Task AddMessageBus_Should_SendToTheEndpointOfEachCheck_When_RegisteredTwice()
    {
        // arrange
        var orders = new Uri("rabbitmq://orders-health");
        var bus = CreateBus(_ => new ValueTask<HealthResponse>(new HealthResponse("OK")));
        var services = CreateServices(bus.Object);
        services.AddHealthChecks().AddMessageBus().AddMessageBus(orders, name: "orders-bus");
        await using var provider = services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<HealthCheckService>();

        // act
        var report = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HealthStatus.Healthy, report.Status);
        bus.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == null),
            It.IsAny<CancellationToken>()), Times.Once());
        bus.Verify(m => m.RequestAsync(
            It.IsAny<HealthRequest>(),
            It.Is<SendOptions>(o => o.Endpoint == orders),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task AddMessageBus_Should_ReportFailureStatusAfterTimeout_When_BusDoesNotAnswer()
    {
        // arrange
        var bus = CreateBus(
            async cancellationToken =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HealthResponse("OK");
            });
        var services = CreateServices(bus.Object);
        services.AddHealthChecks()
            .AddMessageBus(failureStatus: HealthStatus.Degraded, timeout: TimeSpan.FromMilliseconds(100));
        await using var provider = services.BuildServiceProvider();
        var healthChecks = provider.GetRequiredService<HealthCheckService>();

        // act
        var report = await healthChecks.CheckHealthAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var entry = report.Entries["MessageBus"];
        Assert.Equal(HealthStatus.Degraded, entry.Status);
        Assert.Equal("Message Bus did not answer the health request within 00:00:00.1000000.", entry.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddMessageBus_Should_Throw_When_TimeoutIsNotPositive(int seconds)
    {
        // arrange
        var builder = new ServiceCollection().AddHealthChecks();

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.AddMessageBus(timeout: TimeSpan.FromSeconds(seconds)));

        // assert
        Assert.Equal("timeout", exception.ParamName);
    }

    private static ServiceCollection CreateServices(IMessageBus bus)
    {
        var runtime = new Mock<IMessagingRuntime>();
        runtime.SetupGet(r => r.Transports).Returns([]);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(bus);
        services.AddSingleton(runtime.Object);
        return services;
    }

    private static Mock<IMessageBus> CreateBus(Func<CancellationToken, ValueTask<HealthResponse>> respond)
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(m => m.RequestAsync(
                It.IsAny<HealthRequest>(),
                It.IsAny<SendOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEventRequest<HealthResponse> _, SendOptions _, CancellationToken cancellationToken)
                => respond(cancellationToken));
        return bus;
    }
}
