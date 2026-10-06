using System.Data;
using CookieCrumble.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxDataSourceTests(
    PostgresOutboxDataSourceTests.PasswordPostgresResource postgres)
    : IClassFixture<PostgresOutboxDataSourceTests.PasswordPostgresResource>
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
        var connectionString = await CreateDatabaseAsync();
        await using var dataSource = CreateDataSource(connectionString, configuration);
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
    public async Task Worker_Should_ReturnPoolSlot_When_OutboxIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = new NpgsqlConnectionStringBuilder(await CreateDatabaseAsync())
        {
            MaxPoolSize = 1,
            Timeout = 2
        }.ConnectionString;
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, dataSource);
        var worker = provider.GetRequiredService<PostgresMessageBusOutboxWorker>();
        var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        var createConnection = options.CreateConnection;
        options.CreateConnection = services =>
        {
            var connection = createConnection(services);
            connection.StateChange += (_, args) =>
            {
                if (args.OriginalState == ConnectionState.Open && args.CurrentState == ConnectionState.Closed)
                {
                    returned.TrySetResult();
                }
            };
            return connection;
        };

        // act
        object? queryResult;
        string payload;
        try
        {
            await worker.StartAsync(cancellationToken);
            await returned.Task.WaitAsync(s_timeout, cancellationToken);
            await using (var connection = await dataSource.OpenConnectionAsync(cancellationToken))
            {
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                queryResult = await command.ExecuteScalarAsync(cancellationToken);
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IMessageBus>()
                    .PublishAsync(new AuthenticationEvent("after idle"), cancellationToken);
            }

            payload = await provider.GetRequiredService<TaskCompletionSource<string>>().Task
                .WaitAsync(s_timeout, cancellationToken);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(s_timeout, cancellationToken);
        }

        // assert
        Assert.Equal(1, queryResult);
        Assert.Equal("after idle", payload);
        await using var borrowedConnection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var borrowedCommand = new NpgsqlCommand("SELECT 1", borrowedConnection);
        Assert.Equal(1, await borrowedCommand.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var database = $"outbox_auth_{Guid.NewGuid():N}";
        await postgres.CreateDatabaseAsync(database);
        var connectionString = postgres.GetConnectionString(database);
        await using var db = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseTestNpgsql(connectionString).Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        return connectionString;
    }

    private static NpgsqlDataSource CreateDataSource(string connectionString, ConnectionConfiguration configuration)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        if (configuration is ConnectionConfiguration.PeriodicPasswordProvider or ConnectionConfiguration.AsyncPasswordProvider)
        {
            var password = builder.ConnectionStringBuilder.Password!;
            builder.ConnectionStringBuilder.Password = null;
            if (configuration == ConnectionConfiguration.PeriodicPasswordProvider)
            {
                builder.UsePeriodicPasswordProvider(
                    (_, _) => ValueTask.FromResult(password), TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(1));
            }
            else
            {
                builder.UsePasswordProvider(_ => password, (_, _) => ValueTask.FromResult(password));
            }
        }

        return builder.Build();
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

    public enum ConnectionConfiguration
    {
        ConnectionString,
        DataSource,
        PeriodicPasswordProvider,
        AsyncPasswordProvider
    }

    public sealed class PasswordPostgresResource : PostgreSqlResource
    {
        protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
            => builder.WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "scram-sha-256");
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
