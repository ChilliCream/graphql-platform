using Mocha.Sagas;

namespace Mocha.Tests;

/// <summary>
/// Unit tests for <see cref="InMemorySagaTransaction"/> and <see cref="NoOpSagaTransaction"/>.
/// </summary>
public class InMemorySagaTransactionTests
{
    [Fact]
    public void Transaction_StageSave_And_TryGetStagedState_Returns_Staged_Value()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var state = new TestSagaState(Guid.NewGuid(), "Initial") { Data = "staged" };

        // act
        transaction.StageSave("TestSaga", state.Id, state);
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", state.Id, out var staged);

        // assert
        Assert.True(success);
        Assert.Same(state, staged);
    }

    [Fact]
    public void Transaction_StageDelete_And_TryGetStagedState_Returns_Null_Marker()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var id = Guid.NewGuid();

        // act
        transaction.StageDelete("TestSaga", id);
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", id, out var staged);

        // assert
        Assert.True(success);
        Assert.Null(staged);
    }

    [Fact]
    public void Transaction_TryGetStagedState_Unstaged_Key_Returns_False()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);

        // act
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", Guid.NewGuid(), out var staged);

        // assert
        Assert.False(success);
        Assert.Null(staged);
    }

    [Fact]
    public async Task Transaction_CommitAsync_Applies_Saves_To_Storage()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var state = new TestSagaState(Guid.NewGuid(), "Initial") { Data = "committed" };

        // act
        transaction.StageSave("TestSaga", state.Id, state);
        await transaction.CommitAsync(CancellationToken.None);

        // assert
        var loaded = storage.Load<TestSagaState>("TestSaga", state.Id);
        Assert.Same(state, loaded);
    }

    [Fact]
    public async Task Transaction_CommitAsync_Applies_Deletes_To_Storage()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial");
        storage.Save("TestSaga", id, state);

        var transaction = new InMemorySagaTransaction(storage);

        // act
        transaction.StageDelete("TestSaga", id);
        await transaction.CommitAsync(CancellationToken.None);

        // assert
        var loaded = storage.Load<TestSagaState>("TestSaga", id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task Transaction_RollbackAsync_Discards_All_Staged_Changes()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var state = new TestSagaState(Guid.NewGuid(), "Initial") { Data = "should_be_discarded" };

        // act
        transaction.StageSave("TestSaga", state.Id, state);
        await transaction.RollbackAsync(CancellationToken.None);

        // assert - state should not be in storage
        var loaded = storage.Load<TestSagaState>("TestSaga", state.Id);
        Assert.Null(loaded);

        // assert - staged changes should be cleared
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", state.Id, out _);
        Assert.False(success);
    }

    [Fact]
    public async Task Transaction_After_Commit_Staged_Changes_Are_Cleared()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var state = new TestSagaState(Guid.NewGuid(), "Initial");

        // act
        transaction.StageSave("TestSaga", state.Id, state);
        await transaction.CommitAsync(CancellationToken.None);

        // assert - TryGetStagedState should return false after commit
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", state.Id, out _);
        Assert.False(success);
    }

    [Fact]
    public async Task Transaction_After_Rollback_Storage_Is_Unchanged()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var existingId = Guid.NewGuid();
        var existingState = new TestSagaState(existingId, "Existing") { Data = "original" };
        storage.Save("TestSaga", existingId, existingState);

        var transaction = new InMemorySagaTransaction(storage);
        var newId = Guid.NewGuid();
        var newState = new TestSagaState(newId, "New");

        // act - stage changes and rollback
        transaction.StageSave("TestSaga", newId, newState);
        transaction.StageDelete("TestSaga", existingId);
        await transaction.RollbackAsync(CancellationToken.None);

        // assert - new state should not exist
        Assert.Null(storage.Load<TestSagaState>("TestSaga", newId));

        // assert - existing state should still exist
        var loaded = storage.Load<TestSagaState>("TestSaga", existingId);
        Assert.Same(existingState, loaded);
    }

    [Fact]
    public async Task Transaction_Operations_After_Commit_Throw()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        await transaction.CommitAsync(CancellationToken.None);

        // act & assert
        var state = new TestSagaState(Guid.NewGuid(), "Initial");
        Assert.Throws<InvalidOperationException>(() => transaction.StageSave("TestSaga", state.Id, state));
        Assert.Throws<InvalidOperationException>(() => transaction.StageDelete("TestSaga", state.Id));
    }

    [Fact]
    public async Task Transaction_Operations_After_Rollback_Throw()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        await transaction.RollbackAsync(CancellationToken.None);

        // act & assert
        var state = new TestSagaState(Guid.NewGuid(), "Initial");
        Assert.Throws<InvalidOperationException>(() => transaction.StageSave("TestSaga", state.Id, state));
        Assert.Throws<InvalidOperationException>(() => transaction.StageDelete("TestSaga", state.Id));
    }

    [Fact]
    public async Task Transaction_Operations_After_Dispose_Throw()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        await transaction.DisposeAsync();

        // act & assert
        var state = new TestSagaState(Guid.NewGuid(), "Initial");
        Assert.Throws<InvalidOperationException>(() => transaction.StageSave("TestSaga", state.Id, state));
        Assert.Throws<InvalidOperationException>(() => transaction.StageDelete("TestSaga", state.Id));
    }

    [Fact]
    public async Task Transaction_Multiple_Staged_Operations_Commit_In_Correct_Order()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var state1 = new TestSagaState(id1, "State1") { Data = "data1" };
        var state2 = new TestSagaState(id2, "State2") { Data = "data2" };
        var state3 = new TestSagaState(id3, "State3") { Data = "data3" };

        // act - stage multiple operations
        transaction.StageSave("Saga1", id1, state1);
        transaction.StageSave("Saga2", id2, state2);
        transaction.StageSave("Saga3", id3, state3);
        await transaction.CommitAsync(CancellationToken.None);

        // assert - all states should be in storage
        Assert.Same(state1, storage.Load<TestSagaState>("Saga1", id1));
        Assert.Same(state2, storage.Load<TestSagaState>("Saga2", id2));
        Assert.Same(state3, storage.Load<TestSagaState>("Saga3", id3));
    }

    [Fact]
    public async Task Transaction_DisposeAsync_Clears_State_And_Marks_Inactive()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);
        var state = new TestSagaState(Guid.NewGuid(), "Initial");
        transaction.StageSave("TestSaga", state.Id, state);

        // act
        await transaction.DisposeAsync();

        // assert - IsActive should be false
        Assert.False(transaction.IsActive);

        // assert - staged changes should be cleared
        var success = transaction.TryGetStagedState<TestSagaState>("TestSaga", state.Id, out _);
        Assert.False(success);

        // assert - operations should throw
        Assert.Throws<InvalidOperationException>(() => transaction.StageSave("TestSaga", state.Id, state));
    }

    [Fact]
    public async Task Transaction_IsActive_Property_Reflects_State()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var transaction = new InMemorySagaTransaction(storage);

        // assert - initially active
        Assert.True(transaction.IsActive);

        // act - commit
        await transaction.CommitAsync(CancellationToken.None);

        // assert - inactive after commit
        Assert.False(transaction.IsActive);
    }

    [Fact]
    public async Task NoOpTransaction_CommitAsync_Completes_Successfully()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NoOpTransaction_RollbackAsync_Completes_Successfully()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NoOpTransaction_DisposeAsync_Completes_Successfully()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.DisposeAsync();
    }

    [Fact]
    public void NoOpTransaction_Is_Singleton()
    {
        // arrange & act
        var instance1 = NoOpSagaTransaction.Instance;
        var instance2 = NoOpSagaTransaction.Instance;

        // assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task NoOpTransaction_Multiple_Commits_Are_Safe()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.CommitAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NoOpTransaction_Multiple_Rollbacks_Are_Safe()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.RollbackAsync(CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NoOpTransaction_Multiple_Disposes_Are_Safe()
    {
        // arrange
        var transaction = NoOpSagaTransaction.Instance;

        // act & assert - no observable side-effect; NoOpSagaTransaction is
        // intentionally a no-op so "did not throw" is the behavioral contract.
        await transaction.DisposeAsync();
        await transaction.DisposeAsync();
        await transaction.DisposeAsync();
    }

    private class TestSagaState : SagaStateBase
    {
        public TestSagaState() : base() { }

        public TestSagaState(Guid id, string state) : base(id, state) { }

        public string Data { get; set; } = "";
    }
}
