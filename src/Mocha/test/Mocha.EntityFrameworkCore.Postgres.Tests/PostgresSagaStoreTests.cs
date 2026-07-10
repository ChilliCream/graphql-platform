using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresSagaStoreTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture = fixture;
    private readonly List<IDisposable> _disposables = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

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
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial") { Data = "original" };
        await store1.SaveAsync(saga, state, CancellationToken.None);

        // Act
        var loaded = await store2.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.NotNull(loaded);
        loaded.State = "Updated";
        loaded.Data = "modified";
        await store2.SaveAsync(saga, loaded, CancellationToken.None);

        // Assert
        var reloaded = await store1.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal("Updated", reloaded.State);
        Assert.Equal("modified", reloaded.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_ThrowConcurrencyException_When_SavingWithoutLoadOverExistingRecord()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial") { Data = "original" };
        await store1.SaveAsync(saga, state, CancellationToken.None);

        // Act
        var updated = new TestSagaState(id, "Updated") { Data = "modified" };
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => store2.SaveAsync(saga, updated, CancellationToken.None));

        // Assert
        Assert.Equal("The saga state was concurrently created or already exists.", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_Should_ThrowConcurrencyException_When_VersionConflict()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var initial = new TestSagaState(id, "Initial") { Data = "seed" };
        await store1.SaveAsync(saga, initial, TestContext.Current.CancellationToken);

        var fromStore1 = await store1.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        var fromStore2 = await store2.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromStore1);
        Assert.NotNull(fromStore2);

        fromStore1.State = "FromStore1";
        fromStore1.Data = "store1";
        await store1.SaveAsync(saga, fromStore1, TestContext.Current.CancellationToken);

        var replacement = new TestSagaState(id, "FromStore2") { Data = "store2" };

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => store2.SaveAsync(saga, replacement, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("The saga state was modified by another process.", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_Should_Converge_When_ReloadedAndReappliedAfterConflict()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var initial = new TestSagaState(id, "Initial") { Data = "seed" };
        await store1.SaveAsync(saga, initial, TestContext.Current.CancellationToken);

        var fromStore1 = await store1.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        var fromStore2 = await store2.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromStore1);
        Assert.NotNull(fromStore2);

        fromStore1.State = "FromStore1";
        fromStore2.Data = "store2";
        await store1.SaveAsync(saga, fromStore1, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => store2.SaveAsync(saga, fromStore2, TestContext.Current.CancellationToken));

        // Act
        var reloaded = await store2.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded);
        reloaded.Data = fromStore2.Data;
        await store2.SaveAsync(saga, reloaded, TestContext.Current.CancellationToken);

        // Assert
        var final = await store1.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(final);
        Assert.Equal("FromStore1", final.State);
        Assert.Equal("store2", final.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_ThrowConcurrencyException_When_ConcurrentInsertRace()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, store1) = await CreateStoreAsync(connectionString);
        var (_, store2) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();

        var missingFromStore1 = await store1.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        var missingFromStore2 = await store2.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.Null(missingFromStore1);
        Assert.Null(missingFromStore2);

        var fromStore1 = new TestSagaState(id, "FromStore1") { Data = "store1" };
        var fromStore2 = new TestSagaState(id, "FromStore2") { Data = "store2" };
        await store1.SaveAsync(saga, fromStore1, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => store2.SaveAsync(saga, fromStore2, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("The saga state was concurrently created or already exists.", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_Should_AllowMultipleSaves_When_SameScope()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, seedStore) = await CreateStoreAsync(connectionString);
        var (_, store) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var initial = new TestSagaState(id, "Initial") { Data = "seed" };
        await seedStore.SaveAsync(saga, initial, TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);

        // Act
        loaded.State = "FirstSave";
        loaded.Data = "first";
        await store.SaveAsync(saga, loaded, TestContext.Current.CancellationToken);

        var replacement = new TestSagaState(id, "SecondSave") { Data = "second" };
        await store.SaveAsync(saga, replacement, TestContext.Current.CancellationToken);

        // Assert
        var final = await seedStore.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(final);
        Assert.Equal("SecondSave", final.State);
        Assert.Equal("second", final.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_Update_When_LoadedStateIsReplaced()
    {
        // Arrange
        var connectionString = await _fixture.CreateDatabaseAsync();
        var (_, seedStore) = await CreateStoreAsync(connectionString);
        var (_, store) = await CreateStoreAsync(connectionString);
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var initial = new TestSagaState(id, "Initial") { Data = "seed" };
        await seedStore.SaveAsync(saga, initial, TestContext.Current.CancellationToken);

        var loaded = await store.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        var replacement = new TestSagaState(id, "Updated") { Data = "replacement" };

        // Act
        await store.SaveAsync(saga, replacement, TestContext.Current.CancellationToken);

        // Assert
        var final = await seedStore.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(final);
        Assert.Equal("Updated", final.State);
        Assert.Equal("replacement", final.Data);
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
    public async Task SaveAsync_Should_RecreateState_When_PreviousStateWasDeleted()
    {
        // Arrange
        var (_, store) = await CreateStoreAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();
        var initial = new TestSagaState(id, "Initial") { Data = "original" };
        await store.SaveAsync(saga, initial, TestContext.Current.CancellationToken);
        await store.DeleteAsync(saga, id, TestContext.Current.CancellationToken);

        var replacement = new TestSagaState(id, "Recreated") { Data = "replacement" };

        // Act
        await store.SaveAsync(saga, replacement, TestContext.Current.CancellationToken);

        // Assert
        var final = await store.LoadAsync<TestSagaState>(saga, id, TestContext.Current.CancellationToken);
        Assert.NotNull(final);
        Assert.Equal("Recreated", final.State);
        Assert.Equal("replacement", final.Data);
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

    public ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task<(TestDbContext Context, PostgresSagaStore Store)> CreateStoreAsync(
        string? connectionString = null)
    {
        connectionString ??= await _fixture.CreateDatabaseAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;

        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var queries = PostgresSagaStoreQueries.From(new SagaStateTableInfo());
        var store = new PostgresSagaStore(context, queries, TimeProvider.System);

        _disposables.Add(context);
        _disposables.Add(store);

        return (context, store);
    }
}
