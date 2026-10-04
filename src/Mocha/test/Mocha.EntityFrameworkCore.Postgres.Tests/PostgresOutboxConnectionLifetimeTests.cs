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

public sealed class PostgresOutboxConnectionLifetimeTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private string _connectionString = null!;
    private NpgsqlDataSource _dataSource = null!;
    private ConnectionLifetimeDbContext _context = null!;
    private PostgresMessageOutbox _outbox = null!;

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await fixture.CreateDatabaseAsync();
        _dataSource = NpgsqlDataSource.Create(_connectionString);
        _context = CreateContext(_dataSource);
        _outbox = CreateOutbox(_context);
        await _context.Database.EnsureCreatedAsync(TestToken);
        await using var command = _dataSource.CreateCommand("SHOW max_prepared_transactions");
        var maxPreparedTransactions = await command.ExecuteScalarAsync(TestToken);
        Assert.Equal("0", maxPreparedTransactions);
    }

    public async ValueTask DisposeAsync()
    {
        _outbox.Dispose();
        await _context.DisposeAsync();
        await _dataSource.DisposeAsync();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_PersistingRepeatedly(
        bool alreadyOpen,
        bool openedThroughEf)
    {
        // arrange
        if (alreadyOpen)
        {
            await OpenConnectionAsync(openedThroughEf);
        }

        var states = new List<ConnectionState>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await _outbox.PersistAsync(CreateEnvelope(), TestToken);
            states.Add(_context.Database.GetDbConnection().State);
        }

        // assert
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal([expectedState, expectedState, expectedState], states);
        var outboxMessages = await _context.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(3, outboxMessages);
        if (openedThroughEf)
        {
            await _context.Database.CloseConnectionAsync();
            Assert.Equal(ConnectionState.Closed, _context.Database.GetDbConnection().State);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersistAsync_Should_PreserveAtomicity_When_ExplicitTransactionCompletes(bool commit)
    {
        // arrange
        await using var transaction = await _context.Database.BeginTransactionAsync(TestToken);

        // act
        await SaveWriteAsync(_context, 1);
        await _outbox.PersistAsync(CreateEnvelope(), TestToken);
        var stateAfterPersist = _context.Database.GetDbConnection().State;
        var transactionAfterPersist = _context.Database.CurrentTransaction;
        await SaveWriteAsync(_context, 2);
        await _outbox.PersistAsync(CreateEnvelope(), TestToken);
        if (commit)
        {
            await transaction.CommitAsync(TestToken);
        }
        else
        {
            await transaction.RollbackAsync(TestToken);
        }

        // assert
        await using var verification = CreateContext(_dataSource);
        Assert.Equal(ConnectionState.Open, stateAfterPersist);
        Assert.Same(transaction, transactionAfterPersist);
        var expectedCount = commit ? 2 : 0;
        var applicationWrites = await verification.Writes.CountAsync(TestToken);
        Assert.Equal(expectedCount, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestToken);
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
        await using var provider = CreateProvider(_dataSource, pooled);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConnectionLifetimeDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        var factory = provider.GetRequiredService<IDbContextFactory<ConnectionLifetimeDbContext>>();
        ConnectionState stateAfterPersist;
        int firstBackend;
        int secondBackend;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            firstBackend = await SaveFromRepositoryAsync(factory, 1);
            await outbox.PersistAsync(CreateEnvelope(), TestToken);
            stateAfterPersist = context.Database.GetDbConnection().State;
            secondBackend = await SaveFromRepositoryAsync(factory, 2);
            await outbox.PersistAsync(CreateEnvelope(), TestToken);
            if (commit)
            {
                transaction.Complete();
            }
        }

        // assert
        await using var verification = await factory.CreateDbContextAsync(TestToken);
        Assert.Equal(ConnectionState.Closed, stateAfterPersist);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        Assert.Equal(firstBackend, secondBackend);
        var expectedCount = commit ? 2 : 0;
        var applicationWrites = await verification.Writes.CountAsync(TestToken);
        Assert.Equal(expectedCount, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(expectedCount, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_EnlistOpenConnection_When_AmbientTransactionRollsBack(bool openedThroughEf)
    {
        // arrange
        await OpenConnectionAsync(openedThroughEf);
        ConnectionState stateAfterPersist;

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _outbox.PersistAsync(CreateEnvelope(), TestToken);
            stateAfterPersist = _context.Database.GetDbConnection().State;
            await SaveWriteAsync(_context, 1);
        }

        // assert
        await using var verification = CreateContext(_dataSource);
        Assert.Equal(ConnectionState.Open, stateAfterPersist);
        var applicationWrites = await verification.Writes.CountAsync(TestToken);
        Assert.Equal(0, applicationWrites);
        var outboxMessages = await verification.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(0, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_InsertFails(bool alreadyOpen)
    {
        // arrange
        if (alreadyOpen)
        {
            await OpenConnectionAsync(throughEf: true);
        }

        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo { Table = "missing_outbox" });
        using var outbox = new PostgresMessageOutbox(_context, new StubOutboxSignal(), queries.InsertEnvelope);

        // act
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => outbox.PersistAsync(CreateEnvelope(), TestToken).AsTask());
        var stateAfterFailure = _context.Database.GetDbConnection().State;
        await SaveWriteAsync(_context, 1);

        // assert
        Assert.Equal("42P01", exception.SqlState);
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal(expectedState, stateAfterFailure);
        var applicationWrites = await _context.Writes.CountAsync(TestToken);
        Assert.Equal(1, applicationWrites);
        var outboxMessages = await _context.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(0, outboxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistAsync_Should_RestoreConnectionState_When_InsertIsCanceled(bool alreadyOpen)
    {
        // arrange
        if (alreadyOpen)
        {
            await OpenConnectionAsync(throughEf: true);
        }

        await using var blocker = await _dataSource.OpenConnectionAsync(TestToken);
        await using var blockerTransaction = await blocker.BeginTransactionAsync(TestToken);
        await using var lockCommand = blocker.CreateCommand();
        lockCommand.CommandText = "LOCK TABLE outbox_messages IN ACCESS EXCLUSIVE MODE";
        await lockCommand.ExecuteNonQueryAsync(TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        ConnectionState stateAfterCancellation;

        // act
        try
        {
            var persistence = _outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask();
            await WaitForBlockedInsertAsync(_dataSource);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => persistence);
            stateAfterCancellation = _context.Database.GetDbConnection().State;
        }
        finally
        {
            await blockerTransaction.RollbackAsync(CancellationToken.None);
        }

        var messagesAfterCancellation = await _context.Set<OutboxMessage>().CountAsync(TestToken);
        await _outbox.PersistAsync(CreateEnvelope(), TestToken);

        // assert
        var expectedState = alreadyOpen ? ConnectionState.Open : ConnectionState.Closed;
        Assert.Equal(expectedState, stateAfterCancellation);
        Assert.Equal(0, messagesAfterCancellation);
        var messagesAfterRetry = await _context.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(1, messagesAfterRetry);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_CanceledBeforeOpening()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask());
        var stateAfterCancellation = _context.Database.GetDbConnection().State;
        await _outbox.PersistAsync(CreateEnvelope(), TestToken);

        // assert
        Assert.Equal(ConnectionState.Closed, stateAfterCancellation);
        var outboxMessages = await _context.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(1, outboxMessages);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_OpeningFails()
    {
        // arrange
        var settings = new NpgsqlConnectionStringBuilder(_connectionString) { Database = $"missing_{Guid.NewGuid():N}" };
        await using var dataSource = NpgsqlDataSource.Create(settings.ConnectionString);
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        var results = new List<(string SqlState, ConnectionState State)>();

        // act
        for (var i = 0; i < 2; i++)
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => outbox.PersistAsync(CreateEnvelope(), TestToken).AsTask());
            results.Add((exception.SqlState, context.Database.GetDbConnection().State));
        }

        // assert
        Assert.Equal([("3D000", ConnectionState.Closed), ("3D000", ConnectionState.Closed)], results);
    }

    [Fact]
    public async Task PersistAsync_Should_LeaveConnectionClosed_When_OpeningIsCanceled()
    {
        // arrange
        var settings = new NpgsqlConnectionStringBuilder(_connectionString) { MaxPoolSize = 1 };
        await using var dataSource = NpgsqlDataSource.Create(settings.ConnectionString);
        await using var context = CreateContext(dataSource);
        using var outbox = CreateOutbox(context);
        await using var blocker = await dataSource.OpenConnectionAsync(TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);

        // act
        var persistence = outbox.PersistAsync(CreateEnvelope(), cancellation.Token).AsTask();
        var stateWhileOpening = context.Database.GetDbConnection().State;
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => persistence);
        var stateAfterCancellation = context.Database.GetDbConnection().State;
        await blocker.CloseAsync();
        await outbox.PersistAsync(CreateEnvelope(), TestToken);

        // assert
        Assert.Equal(ConnectionState.Connecting, stateWhileOpening);
        Assert.Equal(ConnectionState.Closed, stateAfterCancellation);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        var outboxMessages = await context.Set<OutboxMessage>().CountAsync(TestToken);
        Assert.Equal(1, outboxMessages);
    }

    private Task OpenConnectionAsync(bool throughEf)
        => throughEf
            ? _context.Database.OpenConnectionAsync(TestToken)
            : _context.Database.GetDbConnection().OpenAsync(TestToken);

    private static async Task SaveWriteAsync(ConnectionLifetimeDbContext context, int id)
    {
        context.Writes.Add(new ApplicationWrite { Id = id });
        await context.SaveChangesAsync(TestToken);
    }

    private static async Task<int> SaveFromRepositoryAsync(
        IDbContextFactory<ConnectionLifetimeDbContext> factory,
        int id)
    {
        await using var context = await factory.CreateDbContextAsync(TestToken);
        var backend = await context.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync(TestToken);
        await SaveWriteAsync(context, id);
        return backend;
    }

    private static ConnectionLifetimeDbContext CreateContext(NpgsqlDataSource dataSource)
        => new(new DbContextOptionsBuilder<ConnectionLifetimeDbContext>()
            .UseNpgsql(dataSource)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options);

    private static PostgresMessageOutbox CreateOutbox(DbContext context)
        => new(context, new StubOutboxSignal(),
            PostgresMessageOutboxQueries.From(new OutboxTableInfo()).InsertEnvelope);

    private static ServiceProvider CreateProvider(NpgsqlDataSource dataSource, bool pooled)
    {
        var services = new ServiceCollection();
        void Configure(DbContextOptionsBuilder options)
            => options.UseNpgsql(dataSource)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        if (pooled)
        {
            services.AddPooledDbContextFactory<ConnectionLifetimeDbContext>(Configure);
        }
        else
        {
            services.AddDbContextFactory<ConnectionLifetimeDbContext>(Configure);
        }

        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ConnectionLifetimeDbContext>>().CreateDbContext());
        services.AddMessageBus().AddEntityFramework<ConnectionLifetimeDbContext>(ef => ef.UsePostgresOutbox());
        return services.BuildServiceProvider();
    }

    private static MessageEnvelope CreateEnvelope()
        => new()
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:connection-lifetime",
            DestinationAddress = "memory://test/queue",
            SentAt = DateTimeOffset.UtcNow,
            Body = "{}"u8.ToArray()
        };

    private static async Task WaitForBlockedInsertAsync(NpgsqlDataSource dataSource)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
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
