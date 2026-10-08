using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresSchedulingDataSourceTests(PasswordPostgresResource postgres)
    : IClassFixture<PasswordPostgresResource>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(ConnectionConfiguration.ConnectionString)]
    [InlineData(ConnectionConfiguration.DataSource)]
    [InlineData(ConnectionConfiguration.PeriodicPasswordProvider)]
    [InlineData(ConnectionConfiguration.AsyncPasswordProvider)]
    public async Task Worker_Should_DispatchScheduledMessage_When_ContextUsesPasswordAuthentication(
        ConnectionConfiguration configuration)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await postgres.CreateTestDatabaseAsync();
        await using var dataSource = configuration.CreateDataSource(connectionString);
        await using var provider = await CreateProviderAsync(
            connectionString, configuration == ConnectionConfiguration.ConnectionString ? null : dataSource);
        var delivered = provider.GetRequiredService<TaskCompletionSource<string>>();
        var worker = provider.GetRequiredService<ScheduledMessageWorker>();
        var scheduledCount = await ScheduleAsync(provider, "scheduled");

        // act
        try
        {
            await worker.StartAsync(cancellationToken);
            try
            {
                await delivered.Task.WaitAsync(s_timeout, cancellationToken);
            }
            catch (TimeoutException)
            {
            }
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(s_timeout, cancellationToken);
        }

        // assert
        Assert.Equal(1, scheduledCount);
        Assert.True(
            delivered.Task.IsCompletedSuccessfully,
            $"The scheduled message was not dispatched within {s_timeout.TotalSeconds} seconds.");
        Assert.Equal("scheduled", await delivered.Task);
    }

    [Fact]
    public async Task Worker_Should_ReleaseConnection_When_Idle()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var applicationName = $"scheduling_idle_{Guid.NewGuid():N}";
        // pooling is disabled so a released connection closes its server session
        var connectionString = new NpgsqlConnectionStringBuilder(await postgres.CreateTestDatabaseAsync())
        {
            ApplicationName = applicationName,
            Pooling = false
        }.ConnectionString;
        await using var provider = await CreateProviderAsync(connectionString, dataSource: null);
        var delivered = provider.GetRequiredService<TaskCompletionSource<string>>();
        var worker = provider.GetRequiredService<ScheduledMessageWorker>();
        await ScheduleAsync(provider, "idle");

        // act
        long sessions;
        await worker.StartAsync(cancellationToken);
        try
        {
            await delivered.Task.WaitAsync(s_timeout, cancellationToken);
            sessions = await postgres.WaitForSessionCountAsync(applicationName, expected: 0, s_timeout);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(s_timeout, cancellationToken);
        }

        // assert
        Assert.Equal("idle", await delivered.Task);
        Assert.Equal(0, sessions);
    }

    private static async Task<int> ScheduleAsync(ServiceProvider provider, string payload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(
            new ScheduledEvent(payload),
            new PublishOptions { ScheduledTime = TimeProvider.System.GetUtcNow() },
            cancellationToken);
        return await scope.ServiceProvider.GetRequiredService<TestDbContext>()
            .Set<ScheduledMessage>()
            .CountAsync(cancellationToken);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, NpgsqlDataSource? dataSource)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
        services.AddDbContext<TestDbContext>(options =>
        {
            if (dataSource is null)
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseNpgsql(dataSource);
            }

            options.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        });
        var builder = services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresScheduling())
            .AddEventHandler<ScheduledEventHandler>();
        // the transport is added without its native scheduling store, so Postgres stores scheduled messages
        var transport = new InMemoryMessagingTransport(static _ => { });
        builder.ConfigureMessageBus(b => b.AddTransport(transport));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>())
            .StartAsync(TestContext.Current.CancellationToken);
        return provider;
    }

    public sealed record ScheduledEvent(string Payload);

    public sealed class ScheduledEventHandler(TaskCompletionSource<string> delivered)
        : IEventHandler<ScheduledEvent>
    {
        public ValueTask HandleAsync(ScheduledEvent message, CancellationToken cancellationToken)
        {
            delivered.TrySetResult(message.Payload);
            return ValueTask.CompletedTask;
        }
    }
}
