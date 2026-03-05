using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Npgsql;

namespace Mocha.Sagas.Tests;

public class MessagingTestBase
{
    private readonly ExtendedPostgresResource _resource;
    private readonly ActivityListener _activityListener;

    public MessagingTestBase(ExtendedPostgresResource resource)
    {
        _resource = resource;

        _activityListener = new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public async Task<IServiceProvider> CreateServiceProvider(
        Action<IMessageBusHostBuilder> configureServices,
        string? dbName = null)
    {
        dbName = "DB_" + Guid.NewGuid().ToString("N");
        await _resource.CreateDatabaseAsync(dbName);
        var connectionString = _resource.GetConnectionString(dbName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.MigrateAsync();
        await connection.CloseAsync();

        var services = new ServiceCollection().AddLogging();

        var builder = services.AddMessageBus();
        configureServices(builder);

        return builder.Services.BuildServiceProvider();
    }

    public async Task<IAsyncDisposable> StartBusAsync(IServiceProvider services)
    {
        var runtime = (MessagingRuntime)services.GetRequiredService<IMessagingRuntime>();

        await runtime.StartAsync(CancellationToken.None);

        return new Disposable(async () => await runtime.DisposeAsync());
    }

    private sealed class Disposable(Func<ValueTask> dispose) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await dispose();
        }
    }
}
