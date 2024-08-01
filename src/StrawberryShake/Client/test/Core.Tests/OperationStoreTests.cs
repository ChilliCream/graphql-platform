using Moq;

namespace StrawberryShake;

public class OperationStoreTests
{
    [Fact]
    public void Store_And_Retrieve_Result()
    {
        // arrange
        var entityStore = new EntityStore();
        var document = new Mock<IDocument>();
        var result = new Mock<IOperationResult<string>>();
        var store = new OperationStore(entityStore);
        var request = new OperationRequest("abc", document.Object);

        // act
        store.Set(request, result.Object);
        var success = store.TryGet(request, out IOperationResult<string>? retrieved);

        // assert
        Assert.True(success);
        Assert.Same(result.Object, retrieved);
    }

    [Fact]
    public void TryGet_Not_Found()
    {
        // arrange
        var entityStore = new EntityStore();
        var document = new Mock<IDocument>();
        var store = new OperationStore(entityStore);
        var request = new OperationRequest("abc", document.Object);

        // act
        var success = store.TryGet(request, out IOperationResult<string>? retrieved);

        // assert
        Assert.False(success);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Watch_For_Updates()
    {
        // arrange
        var entityStore = new EntityStore();
        var document = new Mock<IDocument>();
        var result = new Mock<IOperationResult<string>>();
        var store = new OperationStore(entityStore);
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        // act
        using var session = store
            .Watch<string>(request)
            .Subscribe(observer);

        // assert
        store.Set(request, result.Object);
        Assert.Same(result.Object, observer.LastResult);
    }

    [Fact]
    public void Watch_For_Updates_With_SystemReactive()
    {
        // arrange
        var entityStore = new EntityStore();
        var document = new Mock<IDocument>();
        var result = new Mock<IOperationResult<string>>();
        var store = new OperationStore(entityStore);
        var request = new OperationRequest("abc", document.Object);
        IOperationResult<string>? lastResult = null;

        // act
        using var session =
            ObservableExtensions.Subscribe(
                store.Watch<string>(request),
                r =>
                {
                    lastResult = r;
                });

        // assert
        store.Set(request, result.Object);
        Assert.Same(result.Object, lastResult);
    }

    [Fact]
    public void Watch_Unsubscribe()
    {
        // arrange
        var entityStore = new EntityStore();
        var document = new Mock<IDocument>();
        var result = new Mock<IOperationResult<string>>();
        var store = new OperationStore(entityStore);
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var session = store
            .Watch<string>(request)
            .Subscribe(observer);

        // act
        session.Dispose();

        // assert
        store.Set(request, result.Object);
        Assert.Null(observer.LastResult);
    }

    public class ResultObserver : IObserver<IOperationResult<string>>
    {
        public IOperationResult<string>? LastResult { get; private set; }

        public void OnNext(IOperationResult<string> value)
        {
            LastResult = value;
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private sealed class MockEntityChangeObservable
        : IObservable<EntityUpdate>
        , IDisposable
    {
        public IDisposable Subscribe(IObserver<EntityUpdate> observer)
        {
            return this;
        }

        public void Dispose()
        {
        }
    }
}
