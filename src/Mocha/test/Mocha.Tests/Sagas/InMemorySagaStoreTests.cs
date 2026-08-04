using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;

namespace Mocha.Tests;

public class InMemorySagaStoreTests
{
    [Fact]
    public void StorageLoad_Should_ReturnSeparateStates_When_SameIdUsedWithDifferentSagaNames()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id = Guid.NewGuid();
        storage.Save("saga-a", id, new TestSagaState { OrderId = "A" });
        storage.Save("saga-b", id, new TestSagaState { OrderId = "B" });

        // act
        var loadedA = storage.Load<TestSagaState>("saga-a", id);
        var loadedB = storage.Load<TestSagaState>("saga-b", id);

        // assert
        Assert.NotNull(loadedA);
        Assert.NotNull(loadedB);
        Assert.Equal("A", loadedA.OrderId);
        Assert.Equal("B", loadedB.OrderId);
    }

    [Fact]
    public void StoreRegistration_Should_RegisterISagaStore_When_AddInMemorySagasIsCalled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddInMemorySagas();

        var provider = services.BuildServiceProvider();

        // act - ISagaStore is registered as scoped, so we need a scope
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetService<ISagaStore>();

        // assert
        Assert.NotNull(store);
        Assert.IsType<InMemorySagaStore>(store);
    }

    [Fact]
    public void Registration_AddInMemorySagas_Registers_Expected_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddInMemorySagas();
        var provider = services.BuildServiceProvider();

        // assert - InMemorySagaStateStorage should be registered as singleton
        var storage1 = provider.GetService<InMemorySagaStateStorage>();
        var storage2 = provider.GetService<InMemorySagaStateStorage>();
        Assert.NotNull(storage1);
        Assert.Same(storage1, storage2);

        // assert - ISagaStore should be registered as scoped
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var store1 = scope1.ServiceProvider.GetService<ISagaStore>();
        var store2 = scope1.ServiceProvider.GetService<ISagaStore>();
        var store3 = scope2.ServiceProvider.GetService<ISagaStore>();

        Assert.NotNull(store1);
        Assert.NotNull(store3);
        Assert.Same(store1, store2); // Same within scope
        Assert.NotSame(store1, store3); // Different across scopes
        Assert.IsType<InMemorySagaStore>(store1);
    }

    [Fact]
    public void Registration_AddInMemorySagas_Returns_Service_Collection()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        var result = services.AddInMemorySagas();

        // assert
        Assert.Same(services, result);
    }

    [Fact]
    public void Registration_Multiple_Calls_Do_Not_Duplicate_Registrations()
    {
        // arrange
        var services = new ServiceCollection();

        // act - call multiple times
        services.AddInMemorySagas();
        services.AddInMemorySagas();
        services.AddInMemorySagas();

        // assert - should only have one registration of each type
        var storageDescriptors = services.Where(d => d.ServiceType == typeof(InMemorySagaStateStorage)).ToList();
        var storeDescriptors = services.Where(d => d.ServiceType == typeof(ISagaStore)).ToList();

        Assert.Single(storageDescriptors);
        Assert.Single(storeDescriptors);
    }

    [Fact]
    public async Task StoreSaveAndLoadAsync_Should_RoundTrip_When_SaveAndLoadAsyncAreCalled()
    {
        // arrange
        var (store, _, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-100", Amount = 50m };

        // act - save without a transaction (goes directly to storage)
        await store.SaveAsync(saga, state, CancellationToken.None);
        var loaded = await store.LoadAsync<TestSagaState>(saga, state.Id, CancellationToken.None);

        // assert
        Assert.NotNull(loaded);
        Assert.Equal("ORD-100", loaded.OrderId);
        Assert.Equal(50m, loaded.Amount);
        Assert.Equal(state.Id, loaded.Id);
    }

    [Fact]
    public async Task StoreLoadAsync_Should_ReturnNull_When_StateNotFound()
    {
        // arrange
        var (store, _, saga) = CreateStore();

        // act
        var loaded = await store.LoadAsync<TestSagaState>(saga, Guid.NewGuid(), CancellationToken.None);

        // assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task StoreDeleteAsync_Should_RemoveState_When_DeleteAsyncIsCalled()
    {
        // arrange
        var (store, _, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-200" };
        await store.SaveAsync(saga, state, CancellationToken.None);

        // act
        await store.DeleteAsync(saga, state.Id, CancellationToken.None);
        var loaded = await store.LoadAsync<TestSagaState>(saga, state.Id, CancellationToken.None);

        // assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task StoreDeleteAsync_Should_NotThrow_When_StateDoesNotExist()
    {
        // arrange
        var (store, _, saga) = CreateStore();

        // act & assert - should not throw
        await store.DeleteAsync(saga, Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task TransactionStartAsync_Should_ReturnTransaction_When_StartTransactionAsyncIsCalled()
    {
        // arrange
        var (store, _, _) = CreateStore();

        // act
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // assert
        Assert.NotNull(transaction);
    }

    [Fact]
    public async Task TransactionCommit_Should_PersistState_When_CommitAsyncIsCalled()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-1", Amount = 100m };

        // act - start transaction, save, then commit
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.SaveAsync(saga, state, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        // assert - after commit, state should be in storage
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-1", loaded.OrderId);
    }

    [Fact]
    public async Task TransactionRollback_Should_DiscardChanges_When_RollbackAsyncIsCalled()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-2", Amount = 200m };

        // act - start transaction, save, then rollback
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.SaveAsync(saga, state, CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);

        // assert - after rollback, state should NOT be in storage
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task TransactionDispose_Should_CleanUp_When_DisposeAsyncIsCalled()
    {
        // arrange
        var (store, _, _) = CreateStore();

        // act
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await transaction.DisposeAsync();

        // assert - no exception thrown, and a new transaction can be started
        var transaction2 = await store.StartTransactionAsync(CancellationToken.None);
        Assert.NotNull(transaction2);
    }

    [Fact]
    public async Task TransactionLoadAsync_Should_ReadStagedState_When_StateIsSavedInTransaction()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-3", Amount = 300m };

        // act - start transaction, save (staged), then load
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.SaveAsync(saga, state, CancellationToken.None);

        // Load should return the staged state even before commit
        var loaded = await store.LoadAsync<TestSagaState>(saga, state.Id, CancellationToken.None);

        // assert
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-3", loaded.OrderId);

        // But it should NOT be in the underlying storage yet
        var directLoaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.Null(directLoaded);

        await transaction.RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransactionDeleteAsync_Should_StageDeletion_When_DeleteAsyncIsCalledInTransaction()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-4", Amount = 400m };

        // Pre-populate storage directly
        storage.Save(saga.Name, state.Id, state);

        // act - start transaction, delete (staged), then commit
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.DeleteAsync(saga, state.Id, CancellationToken.None);

        // Before commit, state should still be in storage
        var directLoaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(directLoaded);

        await transaction.CommitAsync(CancellationToken.None);

        // After commit, state should be removed from storage
        directLoaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.Null(directLoaded);
    }

    [Fact]
    public async Task TransactionRollbackAfterDelete_Should_PreserveState_When_RollbackAsyncIsCalledAfterDelete()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-5", Amount = 500m };

        // Pre-populate storage
        storage.Save(saga.Name, state.Id, state);

        // act - start transaction, delete (staged), then rollback
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.DeleteAsync(saga, state.Id, CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);

        // assert - after rollback, state should still exist in storage
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-5", loaded.OrderId);
    }

    [Fact]
    public async Task TransactionSecondStart_Should_ReturnNoOp_When_TransactionIsActive()
    {
        // arrange
        var (store, _, _) = CreateStore();

        // act - start first transaction (active), then start second
        var tx1 = await store.StartTransactionAsync(CancellationToken.None);
        var tx2 = await store.StartTransactionAsync(CancellationToken.None);

        // assert - second transaction should be a different (NoOp) instance
        Assert.NotSame(tx1, tx2);

        // Cleanup
        await tx1.CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransactionAfterCommit_Should_StartNewRealTransaction_When_StartTransactionAsyncIsCalledAfterCommit()
    {
        // arrange
        var (store, storage, saga) = CreateStore();

        // First transaction
        var tx1 = await store.StartTransactionAsync(CancellationToken.None);
        await tx1.CommitAsync(CancellationToken.None);

        // act - start a new transaction after commit
        var tx2 = await store.StartTransactionAsync(CancellationToken.None);
        var state = new TestSagaState { OrderId = "ORD-TX-6", Amount = 600m };
        await store.SaveAsync(saga, state, CancellationToken.None);
        await tx2.CommitAsync(CancellationToken.None);

        // assert
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-6", loaded.OrderId);
    }

    [Fact]
    public async Task TransactionAfterRollback_Should_StartNewRealTransaction_When_StartTransactionAsyncIsCalledAfterRollback()
    {
        // arrange
        var (store, storage, saga) = CreateStore();

        // First transaction - rollback
        var tx1 = await store.StartTransactionAsync(CancellationToken.None);
        await tx1.RollbackAsync(CancellationToken.None);

        // act - start new transaction after rollback
        var tx2 = await store.StartTransactionAsync(CancellationToken.None);
        var state = new TestSagaState { OrderId = "ORD-TX-7", Amount = 700m };
        await store.SaveAsync(saga, state, CancellationToken.None);
        await tx2.CommitAsync(CancellationToken.None);

        // assert
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-7", loaded.OrderId);
    }

    [Fact]
    public async Task TransactionAfterDispose_Should_StartNewRealTransaction_When_StartTransactionAsyncIsCalledAfterDispose()
    {
        // arrange
        var (store, storage, saga) = CreateStore();

        // First transaction - dispose
        var tx1 = await store.StartTransactionAsync(CancellationToken.None);
        await tx1.DisposeAsync();

        // act - start new transaction after dispose
        var tx2 = await store.StartTransactionAsync(CancellationToken.None);
        var state = new TestSagaState { OrderId = "ORD-TX-8", Amount = 800m };
        await store.SaveAsync(saga, state, CancellationToken.None);
        await tx2.CommitAsync(CancellationToken.None);

        // assert
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-TX-8", loaded.OrderId);
    }

    [Fact]
    public async Task SaveWithoutTransaction_Should_GoDirectlyToStorage_When_TransactionIsNotStarted()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-DIRECT", Amount = 42m };

        // act - no transaction started
        await store.SaveAsync(saga, state, CancellationToken.None);

        // assert - should be immediately in storage
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ORD-DIRECT", loaded.OrderId);
    }

    [Fact]
    public async Task DeleteWithoutTransaction_Should_GoDirectlyToStorage_When_TransactionIsNotStarted()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-DEL" };
        storage.Save(saga.Name, state.Id, state);

        // act - no transaction started
        await store.DeleteAsync(saga, state.Id, CancellationToken.None);

        // assert - should be immediately removed from storage
        var loaded = storage.Load<TestSagaState>(saga.Name, state.Id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadWithoutTransaction_Should_ReadFromStorage_When_TransactionIsNotStarted()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-LOAD", Amount = 77m };
        storage.Save(saga.Name, state.Id, state);

        // act - no transaction started
        var loaded = await store.LoadAsync<TestSagaState>(saga, state.Id, CancellationToken.None);

        // assert
        Assert.NotNull(loaded);
        Assert.Equal("ORD-LOAD", loaded.OrderId);
        Assert.Equal(77m, loaded.Amount);
    }

    [Fact]
    public async Task TransactionLoadAsyncAfterDelete_Should_ReturnNull_When_DeletionIsStagedInTransaction()
    {
        // arrange
        var (store, storage, saga) = CreateStore();
        var state = new TestSagaState { OrderId = "ORD-TX-DEL", Amount = 999m };
        storage.Save(saga.Name, state.Id, state);

        // act - start transaction, delete (staged), then load
        var transaction = await store.StartTransactionAsync(CancellationToken.None);
        await store.DeleteAsync(saga, state.Id, CancellationToken.None);
        var loaded = await store.LoadAsync<TestSagaState>(saga, state.Id, CancellationToken.None);

        // assert - load should return null because the delete is staged
        Assert.Null(loaded);

        await transaction.RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransactionCommitTwice_Should_BeIdempotent_When_CommitAsyncIsCalledTwice()
    {
        // arrange
        var (store, _, _) = CreateStore();
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // act & assert - should not throw on second commit
        await transaction.CommitAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransactionRollbackTwice_Should_BeIdempotent_When_RollbackAsyncIsCalledTwice()
    {
        // arrange
        var (store, _, _) = CreateStore();
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // act & assert - should not throw on second rollback
        await transaction.RollbackAsync(CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task TransactionDisposeTwice_Should_BeIdempotent_When_DisposeAsyncIsCalledTwice()
    {
        // arrange
        var (store, _, _) = CreateStore();
        var transaction = await store.StartTransactionAsync(CancellationToken.None);

        // act & assert - should not throw on double dispose
        await transaction.DisposeAsync();
        await transaction.DisposeAsync();
    }

    public sealed class TestSagaState : SagaStateBase
    {
        public string OrderId { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public sealed class TestEvent
    {
        public required string OrderId { get; init; }
    }

    public sealed class TestSaga : Saga<TestSagaState>
    {
        protected override void Configure(ISagaDescriptor<TestSagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<TestEvent>()
                .StateFactory(e => new TestSagaState { OrderId = e.OrderId })
                .TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }

    private static (InMemorySagaStore store, InMemorySagaStateStorage storage, TestSaga saga) CreateStore()
    {
        var storage = new InMemorySagaStateStorage();
        var store = new InMemorySagaStore(storage);
        var saga = new TestSaga();
        return (store, storage, saga);
    }
}
