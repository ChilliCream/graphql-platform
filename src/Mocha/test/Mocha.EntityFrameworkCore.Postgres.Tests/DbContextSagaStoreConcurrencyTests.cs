using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

/// <summary>
/// Tests that <see cref="DbContextSagaStore"/> resolves optimistic concurrency conflicts across
/// in-scope retries. A retry re-invokes load and save on the same scoped store, so a load must
/// observe the committed row rather than the instance a failed attempt left in the change tracker.
/// </summary>
public sealed class DbContextSagaStoreConcurrencyTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly List<IAsyncDisposable> _disposables = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveAsync_Should_Succeed_When_RetriedAfterConcurrencyConflict()
    {
        // arrange
        // saga consumer A loads its state, then a concurrent consumer commits first
        var (saga, id, connectionString) = await SeedAsync("Initial");
        var (contextA, storeA) = await CreateStoreAsync(connectionString);
        var (contextB, storeB) = await CreateStoreAsync(connectionString);

        var stateA = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        var stateB = await storeB.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        stateB!.Data = "winner";
        await storeB.SaveAsync(saga, stateB, CancellationToken.None);

        // act
        // the first save conflicts, then the retry re-runs load and save on the same store
        stateA!.Data = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => storeA.SaveAsync(saga, stateA, CancellationToken.None));

        var retried = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        retried!.Data = "retried";
        await storeA.SaveAsync(saga, retried, CancellationToken.None);

        // assert
        var (_, verify) = await CreateStoreAsync(connectionString);
        var committed = await verify.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.Equal("retried", committed!.Data);
        _ = contextA;
        _ = contextB;
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnCommittedState_When_CalledAgainAfterConflictedSave()
    {
        // arrange
        var (saga, id, connectionString) = await SeedAsync("Initial");
        var (_, storeA) = await CreateStoreAsync(connectionString);
        var (_, storeB) = await CreateStoreAsync(connectionString);

        var stateA = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        var stateB = await storeB.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        stateB!.Data = "winner";
        await storeB.SaveAsync(saga, stateB, CancellationToken.None);

        stateA!.Data = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => storeA.SaveAsync(saga, stateA, CancellationToken.None));

        // act
        // the retry's load must observe the committed row, not the failed attempt's write
        var retried = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);

        // assert
        Assert.Equal("winner", retried!.Data);
    }

    [Fact]
    public async Task LoadAsync_Should_ReturnState_When_CalledTwiceOnTheSameStore()
    {
        // arrange
        // a retry after a failure before any save re-runs the load on the same store
        var (saga, id, connectionString) = await SeedAsync("Initial", data: "twice");
        var (_, store) = await CreateStoreAsync(connectionString);

        // act
        var first = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        var second = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);

        // assert
        Assert.Equal("twice", first!.Data);
        Assert.Equal("twice", second!.Data);
    }

    [Fact]
    public async Task SaveAsync_Should_Conflict_When_StateChangedAfterLoad()
    {
        // arrange
        // the guard for the fix: a save must still check the version observed by its own load,
        // so a write that lands between load and save is detected instead of overwritten
        var (saga, id, connectionString) = await SeedAsync("Initial");
        var (_, storeA) = await CreateStoreAsync(connectionString);
        var (_, storeB) = await CreateStoreAsync(connectionString);

        var stateA = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        var stateB = await storeB.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        stateB!.Data = "winner";
        await storeB.SaveAsync(saga, stateB, CancellationToken.None);

        // act & assert
        stateA!.Data = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => storeA.SaveAsync(saga, stateA, CancellationToken.None));

        // assert - the concurrent write survived
        var (_, verify) = await CreateStoreAsync(connectionString);
        var committed = await verify.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        Assert.Equal("winner", committed!.Data);
    }

    [Fact]
    public async Task DeleteAsync_Should_Succeed_When_RetriedAfterConcurrencyConflict()
    {
        // arrange
        // a saga that reaches its final state on a retry deletes through the same store
        var (saga, id, connectionString) = await SeedAsync("Initial");
        var (_, storeA) = await CreateStoreAsync(connectionString);
        var (_, storeB) = await CreateStoreAsync(connectionString);

        var stateA = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        var stateB = await storeB.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        stateB!.Data = "winner";
        await storeB.SaveAsync(saga, stateB, CancellationToken.None);

        stateA!.Data = "loser";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => storeA.SaveAsync(saga, stateA, CancellationToken.None));

        // act - the retry loads fresh state, reaches a final state, and deletes
        _ = await storeA.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        await storeA.DeleteAsync(saga, id, CancellationToken.None);

        // assert
        var (_, verify) = await CreateStoreAsync(connectionString);
        Assert.Null(await verify.LoadAsync<TestSagaState>(saga, id, CancellationToken.None));
    }

    private async Task<(TestSaga Saga, Guid Id, string ConnectionString)> SeedAsync(
        string state,
        string? data = null)
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var saga = new TestSaga();
        var id = Guid.NewGuid();

        var (context, store) = await CreateStoreAsync(connectionString, ensureCreated: true);
        await store.SaveAsync(saga, new TestSagaState(id, state) { Data = data }, CancellationToken.None);
        _ = context;

        return (saga, id, connectionString);
    }

    private async Task<(TestDbContext Context, DbContextSagaStore Store)> CreateStoreAsync(
        string connectionString,
        bool ensureCreated = false)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseTestNpgsql(connectionString).Options;
        var context = new TestDbContext(options);

        if (ensureCreated)
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        var store = new DbContextSagaStore(context);
        _disposables.Add(context);

        return (context, store);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
        {
            await disposable.DisposeAsync();
        }
    }
}
