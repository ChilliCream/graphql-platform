using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Sagas;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresSagaStoreTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture = fixture;
    private readonly List<IDisposable> _disposables = [];

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SaveAsync_Should_InsertNewState_When_NoExistingRecord()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial") { Data = "hello" };

        // Act
        await store.SaveAsync(saga, state, CancellationToken.None);

        // Assert
        var loaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal("Initial", loaded.State);
        Assert.Equal("hello", loaded.Data);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnNull_When_StateDoesNotExist()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();

        // Act
        var loaded = await store.LoadAsync<TestSagaState>(saga, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnState_When_StateExists()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Processing") { Data = "round-trip-value" };
        await store.SaveAsync(saga, state, CancellationToken.None);

        // Act
        var loaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Processing", loaded.State);
        Assert.Equal("round-trip-value", loaded.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_UpdateExistingState_When_RecordExists()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial") { Data = "original" };
        await store.SaveAsync(saga, state, CancellationToken.None);

        // Act
        var updated = new TestSagaState(id, "Updated") { Data = "modified" };
        await store.SaveAsync(saga, updated, CancellationToken.None);

        // Assert
        var loaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Updated", loaded.State);
        Assert.Equal("modified", loaded.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_ThrowConcurrencyException_When_VersionConflict()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (context1, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();

        // Insert initial state so both stores will attempt an UPDATE path.
        var initial = new TestSagaState(id, "Initial") { Data = "seed" };
        await store1.SaveAsync(saga, initial, default);

        // Act — use an explicit transaction on store1 to hold a row lock.
        // Store1 updates within a transaction (reads V1, writes V2, row locked).
        await context1.Database.BeginTransactionAsync(default);
        var fromStore1 = new TestSagaState(id, "FromStore1") { Data = "store1" };
        await store1.SaveAsync(saga, fromStore1, default);

        // Store2 starts its save on a separate connection.
        // In READ COMMITTED, its SELECT sees the committed V1 (store1's tx not committed).
        // Its UPDATE WHERE version=V1 blocks on the row lock held by store1's tx.
        var fromStore2 = new TestSagaState(id, "FromStore2") { Data = "store2" };
        var store2Task = Task.Run(() => store2.SaveAsync(saga, fromStore2, default), default);

        // Give store2 time to SELECT and block on the UPDATE row lock.
        await Task.Delay(200, default);

        // Commit store1's transaction — version changes V1→V2, row lock released.
        // Postgres READ COMMITTED re-evaluates store2's WHERE: version=V1 no longer matches.
        await context1.Database.CommitTransactionAsync(default);

        // Assert — store2's update should find 0 rows and throw.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => store2Task);
    }

    [Fact]
    public async Task DeleteAsync_Should_RemoveState_When_StateExists()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial") { Data = "to-delete" };
        await store.SaveAsync(saga, state, CancellationToken.None);

        // Act
        await store.DeleteAsync(saga, id, CancellationToken.None);

        // Assert
        var loaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteAsync_Should_NoOp_When_StateDoesNotExist()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();

        // Act & Assert - should not throw.
        await store.DeleteAsync(saga, Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task StartTransactionAsync_Should_ReturnEfCoreTransaction_When_NoActiveTransaction()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();

        // Act
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // Assert
        Assert.IsType<EfCoreSagaTransaction>(transaction);

        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task StartTransactionAsync_Should_ReturnNoOpTransaction_When_TransactionAlreadyActive()
    {
        // Arrange
        var (context, store) = await CreateStoreAsync();

        // Start a transaction on the DbContext to simulate an already-active transaction.
        await context.Database.BeginTransactionAsync(CancellationToken.None);

        // Act
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // Assert - when a transaction is already active, the store returns a non-EfCore transaction.
        Assert.IsNotType<EfCoreSagaTransaction>(transaction);

        await transaction.DisposeAsync();
        await context.Database.CurrentTransaction!.DisposeAsync();
    }

    public Task DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }

    private async Task<(TestDbContext Context, PostgresSagaStore Store)> CreateStoreAsync(
        string? connectionString = null)
    {
        connectionString ??= await _fixture.CreateDatabaseAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString).Options;

        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var queries = PostgresSagaStoreQueries.From(new SagaStateTableInfo());
        var store = new PostgresSagaStore(context, queries, TimeProvider.System);

        _disposables.Add(context);
        _disposables.Add(store);

        return (context, store);
    }
}
