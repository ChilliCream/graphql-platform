using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Hosting;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class SharedHealthQueueReproTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HealthCheck_Should_ReportHealthy_When_OnlyAnotherInstanceConsumesHealthRequests()
    {
        // arrange
        // two instances configured like AddNitroMessageBus, sharing one database
        await using var db = await fixture.CreateDatabaseAsync();
        await using var instanceA = await CreateDefaultInstanceAsync(db.ConnectionString);
        await using var instanceB = await CreateDefaultInstanceAsync(db.ConnectionString);

        var endpointNamesA = GetReceiveEndpointNames(instanceA);
        await StopReceiveEndpointAsync(instanceA, "health-request");

        // act
        var withOtherInstance = await CheckHealthAsync(instanceA);
        await StopReceiveEndpointAsync(instanceB, "health-request");
        var withoutOtherInstance = await CheckHealthAsync(instanceA);

        // assert
        new
        {
            EndpointNamesA = endpointNamesA,
            WithOtherInstance = withOtherInstance,
            WithoutOtherInstance = withoutOtherInstance
        }.MatchInlineSnapshot(
            """
            {
              "EndpointNamesA": [
                "Replies",
                "health-request"
              ],
              "WithOtherInstance": "Healthy: Message Bus is healthy.",
              "WithoutOtherInstance": "Unhealthy: A timeout occurred while running check."
            }
            """);
    }

    [Fact]
    public async Task HealthCheck_Should_ReportUnhealthy_When_InstanceQueueIsNotConsumed()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        await using var instanceA = await CreateInstanceWithOwnQueueAsync(db.ConnectionString, "a");
        await using var instanceB = await CreateInstanceWithOwnQueueAsync(db.ConnectionString, "b");

        var endpointNamesA = GetReceiveEndpointNames(instanceA);
        var beforeStop = await CheckHealthAsync(instanceA);
        await StopReceiveEndpointAsync(instanceA, "health-request.a");

        // act
        var afterStop = await CheckHealthAsync(instanceA);

        // assert
        new
        {
            EndpointNamesA = endpointNamesA,
            BeforeStop = beforeStop,
            AfterStop = afterStop
        }.MatchInlineSnapshot(
            """
            {
              "EndpointNamesA": [
                "Replies",
                "health-request.a"
              ],
              "BeforeStop": "Healthy: Message Bus is healthy.",
              "AfterStop": "Unhealthy: A timeout occurred while running check."
            }
            """);
    }

    private static async Task<TestBus> CreateDefaultInstanceAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddMessageBus();
        ShortenHealthCheckTimeout(services);

        return await services
            .AddMessageBus()
            .AddHealthCheck()
            .AddPostgres(t => t.ConnectionString(connectionString))
            .BuildTestBusAsync();
    }

    private static async Task<TestBus> CreateInstanceWithOwnQueueAsync(string connectionString, string instance)
    {
        var queue = $"health-request.{instance}";
        var services = new ServiceCollection();
        services.AddHealthChecks().AddMessageBus(new Uri($"postgres:q/{queue}"));
        ShortenHealthCheckTimeout(services);

        return await services
            .AddMessageBus()
            .AddRequestHandler<InstanceHealthRequestHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(connectionString);
                t.Endpoint(queue).Handler<InstanceHealthRequestHandler>();
            })
            .BuildTestBusAsync();
    }

    private static void ShortenHealthCheckTimeout(IServiceCollection services)
    {
        services.Configure<HealthCheckServiceOptions>(o =>
        {
            foreach (var registration in o.Registrations)
            {
                registration.Timeout = TimeSpan.FromSeconds(5);
            }
        });
    }

    private static async Task<string> CheckHealthAsync(TestBus bus)
    {
        var report = await bus.Provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(TestContext.Current.CancellationToken);
        var entry = report.Entries["MessageBus"];
        return $"{entry.Status}: {entry.Description}";
    }

    private static string[] GetReceiveEndpointNames(TestBus bus)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        return
        [
            .. runtime.Transports
                .SelectMany(t => t.ReceiveEndpoints)
                .Select(e => e.Name)
                .Where(n => !n.StartsWith("response-", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
        ];
    }

    private static async Task StopReceiveEndpointAsync(TestBus bus, string name)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = runtime.Transports.SelectMany(t => t.ReceiveEndpoints).Single(e => e.Name == name);
        await endpoint.StopAsync(runtime, CancellationToken.None);
    }

    public sealed class InstanceHealthRequestHandler : IEventRequestHandler<HealthRequest, HealthResponse>
    {
        public ValueTask<HealthResponse> HandleAsync(HealthRequest request, CancellationToken cancellationToken)
            => new(new HealthResponse("OK"));
    }
}
