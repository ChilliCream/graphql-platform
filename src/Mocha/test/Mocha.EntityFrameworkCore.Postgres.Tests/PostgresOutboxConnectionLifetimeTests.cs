using System.Data;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxConnectionLifetimeTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly string s_insertSql =
        PostgresMessageOutboxQueries.From(new OutboxTableInfo()).InsertEnvelope;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_PersistingRepeatedly(
        bool alreadyOpen,
        bool openedThroughEf)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        var connection = context.Database.GetDbConnection();
        if (alreadyOpen)
        {
            if (openedThroughEf)
            {
                await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
            }
            else
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);
            }
        }

        using var outbox = CreateOutbox(context);
        var states = new List<ConnectionState>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
            states.Add(connection.State);
        }

        // assert
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal([expectedState, expectedState, expectedState], states);
        var outboxMessages = await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, outboxMessages);
        if (openedThroughEf)
        {
            await context.Database.CloseConnectionAsync();
            Assert.Equal(ConnectionState.Closed, connection.State);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersistAsync_Should_PreserveAtomicity_When_ExplicitTransactionCompletes(bool commit)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        await using var transaction =
            await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // act
        context.Writes.Add(new ApplicationWrite { Id = 1 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
        var stateAfterPersist = context.Database.GetDbConnection().State;
        var transactionAfterPersist = context.Database.CurrentTransaction;
        context.Writes.Add(new ApplicationWrite { Id = 2 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
        if (commit)
        {
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        // assert
        await using var verification = CreateContext(dataSource);
        Assert.Equal(ConnectionState.Open, stateAfterPersist);
        Assert.Same(transaction, transactionAfterPersist);
        var expectedCount = commit ? 2 : 0;
        var applicationWrites = await verification.Writes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, outboxMessages);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task PersistAsync_Should_PreserveAtomicity_When_AmbientTransactionUsesFactoryContexts(
        bool commit,
        bool pooled)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        var services = new ServiceCollection();
        if (pooled)
        {
            services.AddPooledDbContextFactory<ConnectionLifetimeDbContext>(o => ConfigureContext(o, dataSource));
        }
        else
        {
            services.AddDbContextFactory<ConnectionLifetimeDbContext>(o => ConfigureContext(o, dataSource));
        }

        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ConnectionLifetimeDbContext>>().CreateDbContext());
        services.AddScoped<IMessageOutbox>(sp => CreateOutbox(sp.GetRequiredService<ConnectionLifetimeDbContext>()));
        await using var provider = services.BuildServiceProvider();
        await using var serviceScope = provider.CreateAsyncScope();
        var scopedContext = serviceScope.ServiceProvider.GetRequiredService<ConnectionLifetimeDbContext>();
        var factory = serviceScope.ServiceProvider.GetRequiredService<IDbContextFactory<ConnectionLifetimeDbContext>>();
        var outbox = serviceScope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        ConnectionState stateAfterPersist;
        int firstBackend;
        int secondBackend;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using (var repositoryContext = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            {
                firstBackend = await GetBackendAsync(repositoryContext);
                repositoryContext.Writes.Add(new ApplicationWrite { Id = 1 });
                await repositoryContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
            stateAfterPersist = scopedContext.Database.GetDbConnection().State;

            await using (var repositoryContext = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
            {
                secondBackend = await GetBackendAsync(repositoryContext);
                repositoryContext.Writes.Add(new ApplicationWrite { Id = 2 });
                await repositoryContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
            if (commit)
            {
                transaction.Complete();
            }
        }

        // assert
        await using var verification = CreateContext(dataSource);
        Assert.Equal(ConnectionState.Closed, stateAfterPersist);
        Assert.Equal(ConnectionState.Closed, scopedContext.Database.GetDbConnection().State);
        Assert.Equal(firstBackend, secondBackend);
        var expectedCount = commit ? 2 : 0;
        var applicationWrites = await verification.Writes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedCount, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_EnlistOpenConnection_When_AmbientTransactionRollsBack(bool openedThroughEf)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        if (openedThroughEf)
        {
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        }
        else
        {
            await context.Database.GetDbConnection().OpenAsync(TestContext.Current.CancellationToken);
        }

        using var outbox = CreateOutbox(context);
        ConnectionState stateAfterPersist;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);
            stateAfterPersist = context.Database.GetDbConnection().State;
            context.Writes.Add(new ApplicationWrite { Id = 1 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // assert
        await using var verification = CreateContext(dataSource);
        Assert.Equal(ConnectionState.Open, stateAfterPersist);
        var applicationWrites = await verification.Writes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_InsertFails(bool alreadyOpen)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        if (alreadyOpen)
        {
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        }

        using var outbox = new PostgresMessageOutbox(
            context,
            new StubOutboxSignal(),
            PostgresMessageOutboxQueries.From(new OutboxTableInfo { Table = "missing_outbox" }).InsertEnvelope);

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken).AsTask());
        var stateAfterFailure = context.Database.GetDbConnection().State;
        context.Writes.Add(new ApplicationWrite { Id = 1 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("42P01", exception.SqlState);
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal(expectedState, stateAfterFailure);
        var applicationWrites = await context.Writes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, applicationWrites);
        var outboxMessages = await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_InsertIsCanceled(bool alreadyOpen)
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        if (alreadyOpen)
        {
            await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        }

        using var outbox = CreateOutbox(context);
        await using var blocker = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var blockerTransaction = await blocker.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using var lockCommand = blocker.CreateCommand();
        lockCommand.CommandText = "LOCK TABLE outbox_messages IN ACCESS EXCLUSIVE MODE";
        await lockCommand.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        ConnectionState stateAfterCancellation;

        // act
        try
        {
            var persistence = outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask();
            await WaitForBlockedInsertAsync(dataSource);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => persistence);
            stateAfterCancellation = context.Database.GetDbConnection().State;
        }
        finally
        {
            await blockerTransaction.RollbackAsync(CancellationToken.None);
        }

        var messagesAfterCancellation =
            await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);

        // assert
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal(expectedState, stateAfterCancellation);
        Assert.Equal(0, messagesAfterCancellation);
        var messagesAfterRetry = await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, messagesAfterRetry);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_CanceledBeforeOpening()
    {
        // arrange
        await using var dataSource = await CreateDataSourceAsync();
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask());
        var stateAfterCancellation = context.Database.GetDbConnection().State;
        await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(ConnectionState.Closed, stateAfterCancellation);
        var outboxMessages = await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, outboxMessages);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_OpeningFails()
    {
        // arrange
        var settings = new NpgsqlConnectionStringBuilder(await fixture.CreateDatabaseAsync())
        {
            Database = $"missing_{Guid.NewGuid():N}"
        };
        await using var dataSource = NpgsqlDataSource.Create(settings.ConnectionString);
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        var results = new List<(string SqlState, ConnectionState State)>();

        // act
        for (var i = 0; i < 2; i++)
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken).AsTask());
            results.Add((exception.SqlState, context.Database.GetDbConnection().State));
        }

        // assert
        Assert.Equal([("3D000", ConnectionState.Closed), ("3D000", ConnectionState.Closed)], results);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_OpeningIsCanceled()
    {
        // arrange
        var settings = new NpgsqlConnectionStringBuilder(await fixture.CreateDatabaseAsync()) { MaxPoolSize = 1 };
        await using var dataSource = NpgsqlDataSource.Create(settings.ConnectionString);
        await InitializeDataSourceAsync(dataSource);
        await using var blocker = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // act
        var persistence = outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask();
        var stateWhileOpening = context.Database.GetDbConnection().State;
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => persistence);
        var stateAfterCancellation = context.Database.GetDbConnection().State;
        await blocker.CloseAsync();
        await outbox.PersistAsync(CreateEnvelope(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(ConnectionState.Connecting, stateWhileOpening);
        Assert.Equal(ConnectionState.Closed, stateAfterCancellation);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        var outboxMessages = await context.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, outboxMessages);
    }

    private async Task<NpgsqlDataSource> CreateDataSourceAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await InitializeDataSourceAsync(dataSource);
        return dataSource;
    }

    private static async Task InitializeDataSourceAsync(NpgsqlDataSource dataSource)
    {
        await using var context = CreateContext(dataSource);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await using var command = dataSource.CreateCommand("SHOW max_prepared_transactions");
        var maxPreparedTransactions = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        Assert.Equal("0", maxPreparedTransactions);
    }

    private static ConnectionLifetimeDbContext CreateContext(NpgsqlDataSource dataSource)
    {
        var builder = new DbContextOptionsBuilder<ConnectionLifetimeDbContext>();
        ConfigureContext(builder, dataSource);
        return new ConnectionLifetimeDbContext(builder.Options);
    }

    private static void ConfigureContext(DbContextOptionsBuilder builder, NpgsqlDataSource dataSource)
        => builder.UseNpgsql(dataSource)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

    private static PostgresMessageOutbox CreateOutbox(DbContext context)
        => new(context, new StubOutboxSignal(), s_insertSql);

    private static MessageEnvelope CreateEnvelope()
        => new()
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:connection-lifetime",
            DestinationAddress = "memory://test/queue",
            SentAt = DateTimeOffset.UtcNow,
            Body = "{}"u8.ToArray()
        };

    private static Task<int> GetBackendAsync(ConnectionLifetimeDbContext context)
        => context.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"")
            .SingleAsync(TestContext.Current.CancellationToken);

    private static async Task WaitForBlockedInsertAsync(NpgsqlDataSource dataSource)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await using var command = dataSource.CreateCommand(
            """
            SELECT EXISTS (
                SELECT 1 FROM pg_stat_activity
                WHERE datname = current_database()
                    AND wait_event_type = 'Lock'
                    AND query LIKE '%outbox_messages%'
            )
            """);
        while (await command.ExecuteScalarAsync(timeout.Token) is not true)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    public sealed class ConnectionLifetimeDbContext(DbContextOptions<ConnectionLifetimeDbContext> options)
        : DbContext(options)
    {
        public DbSet<ApplicationWrite> Writes => Set<ApplicationWrite>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<ApplicationWrite>().ToTable("application_writes");
        }
    }

    public sealed class ApplicationWrite
    {
        public int Id { get; set; }
    }
}
