using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Postgres.Tests.Helpers;
using Npgsql;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public sealed class ShutdownTests(PostgresFixture fixture)
{
    [Fact]
    public async Task StopAsync_Should_UnregisterConsumer_When_HostStops()
    {
        // arrange
        await using var db = await fixture.CreateDatabaseAsync("shutdown_unregister");
        await using var provider = CreateProvider(db.ConnectionString);
        var services = provider.GetServices<IHostedService>().ToArray();
        await StartAsync(services);
        var consumersBeforeStop = await CountConsumersAsync(db.ConnectionString);

        // act
        await StopAsync(services);

        // assert
        Assert.Equal(1, consumersBeforeStop);
        Assert.Equal(0, await CountConsumersAsync(db.ConnectionString));
    }

    private static ServiceProvider CreateProvider(string connectionString)
        => new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<NoOpHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(connectionString);
                t.Endpoint("shutdown").Handler<NoOpHandler>();
            })
            .Services.BuildServiceProvider();

    private static async Task StartAsync(IHostedService[] services)
    {
        foreach (var service in services)
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task StopAsync(IHostedService[] services)
    {
        foreach (var service in services.Reverse())
        {
            await service.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task<long> CountConsumersAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM mocha_consumers";
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    public sealed class TestEvent;

    public sealed class NoOpHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
