using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Mocha.Transport.InMemory;
using Moq;

namespace Mocha.Hosting.Tests.Health;

public sealed class MessageBusTransportsHealthCheckTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_When_EveryComponentIsHealthy()
    {
        // arrange
        var check = CreateCheck(
            new HealthReportingTransport(
                "rabbitmq",
                Entry("connection:consumer", TransportHealthStatus.Healthy, "Connected"),
                Entry("consumer:orders", TransportHealthStatus.Healthy, "Attached")));

        // act
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Summarize(result).MatchInlineSnapshot(
            """
            {
              "Status": "Healthy",
              "Description": "Message bus transports are healthy.",
              "Exception": null,
              "Data": {
                "rabbitmq:connection:consumer": "Connected",
                "rabbitmq:consumer:orders": "Attached"
              }
            }
            """);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_When_ComponentRecovers()
    {
        // arrange
        var check = CreateCheck(
            new HealthReportingTransport(
                "rabbitmq",
                Entry("connection:consumer", TransportHealthStatus.Healthy, "Connected"),
                Entry(
                    "consumer:orders",
                    TransportHealthStatus.Degraded,
                    "Not attached: 404 NOT_FOUND",
                    since: s_now.AddSeconds(-30),
                    exception: new InvalidOperationException("queue missing"))));

        // act
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Summarize(result).MatchInlineSnapshot(
            """
            {
              "Status": "Degraded",
              "Description": "rabbitmq:consumer:orders Not attached: 404 NOT_FOUND",
              "Exception": "queue missing",
              "Data": {
                "rabbitmq:connection:consumer": "Connected",
                "rabbitmq:consumer:orders": "Not attached: 404 NOT_FOUND"
              }
            }
            """);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_When_ComponentDegradedLongerThanThreshold()
    {
        // arrange
        var check = CreateCheck(
            new HealthReportingTransport(
                "rabbitmq",
                Entry(
                    "connection:consumer",
                    TransportHealthStatus.Degraded,
                    "Not connected: Connection refused",
                    since: s_now.AddMinutes(-3))),
            unhealthyAfter: TimeSpan.FromMinutes(2));

        // act
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("rabbitmq:connection:consumer Not connected: Connection refused", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReportWorstStatus_When_TransportsDiffer()
    {
        // arrange
        var check = CreateCheck(
            new HealthReportingTransport(
                "rabbitmq",
                Entry("consumer:orders", TransportHealthStatus.Degraded, "Not attached", since: s_now)),
            new HealthReportingTransport(
                "postgres",
                Entry("connection", TransportHealthStatus.Unhealthy, "Credentials refused")));

        // act
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(
            "rabbitmq:consumer:orders Not attached; postgres:connection Credentials refused",
            result.Description);
    }

    [Fact]
    public async Task AddMessageBusTransports_Should_RegisterHealthyCheck_When_TransportReportsNoComponents()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddInMemory();
        services.AddHealthChecks().AddMessageBusTransports();
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);

        // act
        var report = await provider.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(TestContext.Current.CancellationToken);

        // assert
        var entry = Assert.Contains("MessageBusTransports", report.Entries);
        Assert.Equal(HealthStatus.Healthy, entry.Status);
        Assert.Equal(["ready", "live"], entry.Tags);
    }

    private static MessageBusTransportsHealthCheck CreateCheck(
        HealthReportingTransport transport,
        TimeSpan? unhealthyAfter = null)
        => CreateCheck([transport], unhealthyAfter);

    private static MessageBusTransportsHealthCheck CreateCheck(
        HealthReportingTransport first,
        HealthReportingTransport second)
        => CreateCheck([first, second], unhealthyAfter: null);

    private static MessageBusTransportsHealthCheck CreateCheck(
        MessagingTransport[] transports,
        TimeSpan? unhealthyAfter)
    {
        var runtime = new Mock<IMessagingRuntime>();
        runtime.SetupGet(r => r.Transports).Returns([.. transports]);

        var options = new MessageBusTransportsHealthCheckOptions();
        if (unhealthyAfter is { } threshold)
        {
            options.UnhealthyAfter = threshold;
        }

        return new MessageBusTransportsHealthCheck(runtime.Object, new FakeTimeProvider(s_now), Options.Create(options));
    }

    private static TransportHealthEntry Entry(
        string name,
        TransportHealthStatus status,
        string description,
        DateTimeOffset? since = null,
        Exception? exception = null)
        => new()
        {
            Name = name,
            Status = status,
            Description = description,
            Since = since,
            Exception = exception
        };

    private static object Summarize(HealthCheckResult result)
        => new { result.Status, result.Description, Exception = result.Exception?.Message, result.Data };

    private sealed class HealthReportingTransport : MessagingTransport
    {
        private readonly TransportHealth _health;

        public HealthReportingTransport(string name, params TransportHealthEntry[] entries)
        {
            Name = name;
            _health = new TransportHealth(entries);
        }

        public override MessagingTopology Topology => throw new NotSupportedException();

        public override TransportHealth GetHealth() => _health;

        public override bool TryGetDispatchEndpoint(Uri address, [NotNullWhen(true)] out DispatchEndpoint? endpoint)
            => throw new NotSupportedException();

        protected override MessagingTransportConfiguration CreateConfiguration(IMessagingSetupContext context)
            => throw new NotSupportedException();

        protected override ReceiveEndpoint CreateReceiveEndpoint() => throw new NotSupportedException();

        protected override DispatchEndpoint CreateDispatchEndpoint() => throw new NotSupportedException();
    }
}
