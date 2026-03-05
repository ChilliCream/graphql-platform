using Mocha.Sagas;

namespace Mocha.Tests;

/// <summary>
/// Unit tests for <see cref="InMemorySagaStateStorage"/> — pure storage CRUD operations.
/// </summary>
public class InMemorySagaStateStorageTests
{
    [Fact]
    public void Storage_Save_And_Load_Returns_Same_State()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var state = new TestSagaState(Guid.NewGuid(), "Initial") { Data = "test" };

        // act
        storage.Save("TestSaga", state.Id, state);
        var loaded = storage.Load<TestSagaState>("TestSaga", state.Id);

        // assert
        Assert.Same(state, loaded);
    }

    [Fact]
    public void Storage_Load_NonExistent_Returns_Null()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();

        // act
        var loaded = storage.Load<TestSagaState>("TestSaga", Guid.NewGuid());

        // assert
        Assert.Null(loaded);
    }

    [Fact]
    public void Storage_Delete_And_Load_Returns_Null()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial");
        storage.Save("TestSaga", id, state);

        // act
        storage.Delete("TestSaga", id);
        var loaded = storage.Load<TestSagaState>("TestSaga", id);

        // assert
        Assert.Null(loaded);
    }

    [Fact]
    public void Storage_Save_Overwrites_Existing_State()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id = Guid.NewGuid();
        var state1 = new TestSagaState(id, "State1") { Data = "first" };
        var state2 = new TestSagaState(id, "State2") { Data = "second" };

        // act
        storage.Save("TestSaga", id, state1);
        storage.Save("TestSaga", id, state2);
        var loaded = storage.Load<TestSagaState>("TestSaga", id);

        // assert
        Assert.Same(state2, loaded);
        Assert.Equal("second", loaded?.Data);
    }

    [Fact]
    public void Storage_Clear_Removes_All_States()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        storage.Save("Saga1", id1, new TestSagaState(id1, "State1"));
        storage.Save("Saga2", id2, new TestSagaState(id2, "State2"));
        Assert.Equal(2, storage.Count);

        // act
        storage.Clear();

        // assert
        Assert.Equal(0, storage.Count);
        Assert.Null(storage.Load<TestSagaState>("Saga1", id1));
        Assert.Null(storage.Load<TestSagaState>("Saga2", id2));
    }

    [Fact]
    public void Storage_Count_Reflects_Number_Of_Stored_States()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        Assert.Equal(0, storage.Count);

        // act & assert - add states
        storage.Save("Saga1", Guid.NewGuid(), new TestSagaState());
        Assert.Equal(1, storage.Count);

        storage.Save("Saga2", Guid.NewGuid(), new TestSagaState());
        Assert.Equal(2, storage.Count);

        storage.Save("Saga3", Guid.NewGuid(), new TestSagaState());
        Assert.Equal(3, storage.Count);
    }

    [Fact]
    public async Task Storage_ThreadSafety_Concurrent_Save_And_Load()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var tasks = new List<Task>();
        const int stateCount = 100;

        // act - concurrent saves
        for (int i = 0; i < stateCount; i++)
        {
            var localI = i;
            tasks.Add(
                Task.Run(() =>
                {
                    var id = Guid.NewGuid();
                    var state = new TestSagaState(id, $"State{localI}") { Data = $"Data{localI}" };
                    storage.Save($"Saga{localI}", id, state);
                }, default));
        }

        await Task.WhenAll(tasks);

        // assert
        Assert.Equal(stateCount, storage.Count);
    }

    [Fact]
    public void Storage_Load_WrongType_Returns_Null()
    {
        // arrange
        var storage = new InMemorySagaStateStorage();
        var id = Guid.NewGuid();
        var state = new TestSagaState(id, "Initial");
        storage.Save("TestSaga", id, state);

        // act - try to load as wrong type (base class)
        var loaded = storage.Load<SagaStateBase>("TestSaga", id);

        // assert - should succeed as TestSagaState derives from SagaStateBase
        Assert.NotNull(loaded);
        Assert.IsType<TestSagaState>(loaded);
    }

    private class TestSagaState : SagaStateBase
    {
        public TestSagaState() : base() { }

        public TestSagaState(Guid id, string state) : base(id, state) { }

        public string Data { get; set; } = "";
    }
}
