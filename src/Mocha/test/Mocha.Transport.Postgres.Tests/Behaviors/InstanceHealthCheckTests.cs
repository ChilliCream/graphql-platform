using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mocha.Hosting;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class InstanceHealthCheckTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HealthCheck_Should_ReportUnhealthy_When_OwnEndpointIsStoppedAndAnotherInstanceIsRunning()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        await using var instanceA = await CreateInstanceAsync(db.ConnectionString);
        await using var instanceB = await CreateInstanceAsync(db.ConnectionString);

        var endpointNamesA = GetReceiveEndpointNames(instanceA);
        var beforeStop = await CheckHealthAsync(instanceA);
        await StopHealthEndpointAsync(instanceA);

        // act
        var instanceAfterStop = await CheckHealthAsync(instanceA);
        var otherInstanceAfterStop = await CheckHealthAsync(instanceB);

        // assert
        new
        {
            EndpointNamesA = endpointNamesA,
            BeforeStop = beforeStop,
            InstanceAfterStop = instanceAfterStop,
            OtherInstanceAfterStop = otherInstanceAfterStop
        }.MatchInlineSnapshot(
            """
            {
              "EndpointNamesA": [
                "Replies",
                "health-request-{instance}"
              ],
              "BeforeStop": "Healthy: Message Bus is healthy.",
              "InstanceAfterStop": "Unhealthy: A timeout occurred while running check.",
              "OtherInstanceAfterStop": "Healthy: Message Bus is healthy."
            }
            """);
    }

    [Fact]
    public async Task HealthCheck_Should_ReportUnhealthy_When_ConfiguredEndpointIsNotConsumed()
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

    [Fact]
    public async Task StopAsync_Should_RemoveHealthQueue_When_InstanceStops()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync();
        var instance = await CreateInstanceAsync(db.ConnectionString);
        var queueName = GetHealthEndpoint(instance).Name;
        var beforeStop = await QueueExistsAsync(db.ConnectionString, queueName);

        // act
        await instance.DisposeAsync();

        // assert
        var afterStop = await QueueExistsAsync(db.ConnectionString, queueName);
        Assert.Equal((true, false), (beforeStop, afterStop));
    }

    private static async Task<TestBus> CreateInstanceAsync(string connectionString)
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
        var instanceId = runtime.Host.InstanceId.ToString("N");
        return
        [
            .. runtime.Transports
                .SelectMany(t => t.ReceiveEndpoints)
                .Select(e => e.Name.Replace(instanceId, "{instance}"))
                .Order(StringComparer.Ordinal)
        ];
    }

    private static ReceiveEndpoint GetHealthEndpoint(TestBus bus)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        return runtime.Router.InboundRoutes.Single(r => r.IsInstanceScoped).Endpoint!;
    }

    private static async Task StopHealthEndpointAsync(TestBus bus)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        await GetHealthEndpoint(bus).StopAsync(runtime, CancellationToken.None);
    }

    private static async Task StopReceiveEndpointAsync(TestBus bus, string name)
    {
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = runtime.Transports.SelectMany(t => t.ReceiveEndpoints).Single(e => e.Name == name);
        await endpoint.StopAsync(runtime, CancellationToken.None);
    }

    private static async Task<bool> QueueExistsAsync(string connectionString, string queueName)
    {
        var schema = new PostgresSchemaOptions();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {schema.QueueTable} q WHERE q.name = @queue_name)";
        command.Parameters.AddWithValue("queue_name", queueName);
        return (bool)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    public sealed class InstanceHealthRequestHandler : IEventRequestHandler<HealthRequest, HealthResponse>
    {
        public ValueTask<HealthResponse> HandleAsync(HealthRequest request, CancellationToken cancellationToken)
            => new(new HealthResponse("OK"));
    }
}
