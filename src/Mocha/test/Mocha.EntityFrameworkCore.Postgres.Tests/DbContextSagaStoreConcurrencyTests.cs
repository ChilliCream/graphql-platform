using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

/// <summary>
/// Tests the concurrency contract of <see cref="DbContextSagaStore"/>: a save checks the version its
/// own load observed, and loading twice on one store stays safe because the tracked entity keeps
/// ownership of its document. A retry runs in its own scope and never re-reads through a used store.
/// </summary>
public sealed class DbContextSagaStoreConcurrencyTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly List<IAsyncDisposable> _disposables = [];

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task LoadAsync_Should_ReturnState_When_CalledTwiceOnTheSameStore()
    {
        // arrange
        // the tracked entity keeps its document after a load, so a second load must not touch a
        // disposed document
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
    public async Task LoadAsync_Should_ReturnSavedState_When_CalledAfterSaveOnTheSameStore()
    {
        // arrange
        // a save replaces and releases the loaded document; the entity must remain readable after
        var (saga, id, connectionString) = await SeedAsync("Initial", data: "before");
        var (_, store) = await CreateStoreAsync(connectionString);

        var loaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);
        loaded!.Data = "after";
        await store.SaveAsync(saga, loaded, CancellationToken.None);

        // act
        var reloaded = await store.LoadAsync<TestSagaState>(saga, id, CancellationToken.None);

        // assert
        Assert.Equal("after", reloaded!.Data);
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
