using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxDataSourceTests(PasswordPostgresResource postgres)
    : IClassFixture<PasswordPostgresResource>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(ConnectionConfiguration.ConnectionString)]
    [InlineData(ConnectionConfiguration.DataSource)]
    [InlineData(ConnectionConfiguration.PeriodicPasswordProvider)]
    [InlineData(ConnectionConfiguration.AsyncPasswordProvider)]
    public async Task Worker_Should_DeliverCommittedMessage_When_ContextUsesPasswordAuthentication(
        ConnectionConfiguration configuration)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await postgres.CreateTestDatabaseAsync();
        await using var dataSource = configuration.CreateDataSource(connectionString);
        await using var provider = await CreateProviderAsync(
            connectionString, configuration == ConnectionConfiguration.ConnectionString ? null : dataSource);
        var delivered = provider.GetRequiredService<TaskCompletionSource<string>>();
        var worker = provider.GetRequiredService<PostgresMessageBusOutboxWorker>();

        // act
        int committedCount;
        bool exposesPassword;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            exposesPassword = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Password is not null;
            await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
            {
                await scope.ServiceProvider.GetRequiredService<IMessageBus>()
                    .PublishAsync(new AuthenticationEvent("committed"), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            committedCount = await db.Set<OutboxMessage>().CountAsync(cancellationToken);
        }

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
        Assert.False(new NpgsqlConnectionStringBuilder(connectionString).ShouldSerialize("Persist Security Info"));
        Assert.Equal(1, committedCount);
        Assert.Equal(configuration == ConnectionConfiguration.ConnectionString, exposesPassword);
        Assert.True(
            delivered.Task.IsCompletedSuccessfully,
            $"The committed outbox message was not delivered within {s_timeout.TotalSeconds} seconds. "
                + $"EF connection string exposes a password: {exposesPassword}.");
        Assert.Equal("committed", await delivered.Task);
    }

    [Fact]
    public async Task Worker_Should_ReleaseConnection_When_Idle()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var applicationName = $"outbox_idle_{Guid.NewGuid():N}";
        // pooling is disabled so a released connection closes its server session
        var connectionString = new NpgsqlConnectionStringBuilder(await postgres.CreateTestDatabaseAsync())
        {
            ApplicationName = applicationName,
            Pooling = false
        }.ConnectionString;
        await using var provider = await CreateProviderAsync(connectionString, dataSource: null);
        var delivered = provider.GetRequiredService<TaskCompletionSource<string>>();
        var worker = provider.GetRequiredService<PostgresMessageBusOutboxWorker>();
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>()
                .PublishAsync(new AuthenticationEvent("idle"), cancellationToken);
        }

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
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox())
            .AddEventHandler<AuthenticationEventHandler>()
            .AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>())
            .StartAsync(TestContext.Current.CancellationToken);
        return provider;
    }

    public sealed record AuthenticationEvent(string Payload);

    public sealed class AuthenticationEventHandler(TaskCompletionSource<string> delivered)
        : IEventHandler<AuthenticationEvent>
    {
        public ValueTask HandleAsync(AuthenticationEvent message, CancellationToken cancellationToken)
        {
            delivered.TrySetResult(message.Payload);
            return ValueTask.CompletedTask;
        }
    }
}
