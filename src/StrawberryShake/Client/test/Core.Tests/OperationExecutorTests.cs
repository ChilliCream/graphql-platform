using System.Reactive.Linq;
using Moq;

namespace StrawberryShake;

public class OperationExecutorTests
{
    [Fact]
    public async Task Watch_NetworkOnly_ValueInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var cacheResult = "cache result";
        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = Mock.Of<IOperationResult<string>>(f => f.Data == cacheResult);
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(true);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.NetworkOnly)
            .Subscribe(observer);

        // assert
        var actualNetworkResult = await observer.WaitForResult();
        Assert.Equal(networkResult, actualNetworkResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    [Fact]
    public async Task Watch_NetworkOnly_ValueNotInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = null as IOperationResult<string>;
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(false);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.NetworkOnly)
            .Subscribe(observer);

        // assert
        var actualNetworkResult = await observer.WaitForResult();
        Assert.Equal(networkResult, actualNetworkResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    [Fact]
    public async Task Watch_CacheAndNetwork_ValueInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var cacheResult = "cache result";
        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = Mock.Of<IOperationResult<string>>(f => f.Data == cacheResult);
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(true);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.CacheAndNetwork)
            .Subscribe(observer);

        // assert
        var actualCacheResult = await observer.WaitForResult();
        Assert.Equal(cacheResult, actualCacheResult.Data);

        var actualNetworkResult = await observer.WaitForResult();
        Assert.Equal(networkResult, actualNetworkResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    [Fact]
    public async Task Watch_CacheAndNetwork_ValueNotInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = null as IOperationResult<string>;
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(false);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.CacheAndNetwork)
            .Subscribe(observer);

        // assert
        var actualNetworkResult = await observer.WaitForResult();
        Assert.Equal(networkResult, actualNetworkResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    [Fact]
    public async Task Watch_CacheFirst_ValueInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var cacheResult = "cache result";
        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = Mock.Of<IOperationResult<string>>(f => f.Data == cacheResult);
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(true);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.CacheFirst)
            .Subscribe(observer);

        // assert
        var actualCacheResult = await observer.WaitForResult();
        Assert.Equal(cacheResult, actualCacheResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    [Fact]
    public async Task Watch_CacheFirst_ValueNotInCache()
    {
        // arrange
        var connection = new Mock<IConnection<string>>();
        var operationStore = new Mock<IOperationStore>();
        var resultBuilder = new MockResultBuilder();
        var resultPatcher = new Mock<IResultPatcher<string>>();
        var document = new Mock<IDocument>();
        var request = new OperationRequest("abc", document.Object);
        var observer = new ResultObserver();

        var networkResult = "network result";
        var storeUpdateResult = "store result";

        var executor = new OperationExecutor<string, string>(
            connection.Object,
            () => resultBuilder,
            () => resultPatcher.Object,
            operationStore.Object);

        var cacheOperationResult = null as IOperationResult<string>;
        operationStore.Setup(e => e.TryGet(request, out cacheOperationResult))
            .Returns(false);

        connection.Setup(e => e.ExecuteAsync(request))
            .Returns(ToAsyncEnumerable(new Response<string>(networkResult, null)));

        operationStore.Setup(e => e.Watch<string>(request))
            .Returns(Observable.Return(Mock.Of<IOperationResult<string>>(e => e.Data == storeUpdateResult)));

        // act
        executor.Watch(request, ExecutionStrategy.CacheFirst)
            .Subscribe(observer);

        // assert
        var actualNetworkResult = await observer.WaitForResult();
        Assert.Equal(networkResult, actualNetworkResult.Data);

        var actualStoreUpdateResult = await observer.WaitForResult();
        Assert.Equal(storeUpdateResult, actualStoreUpdateResult.Data);
    }

    private static async IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(params TValue[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }

        await Task.CompletedTask;
    }

    private sealed class MockResultBuilder : IOperationResultBuilder<string, string>
    {
        public IOperationResult<string> Build(Response<string> response)
        {
            return Mock.Of<IOperationResult<string>>(e => e.Data == response.Body);
        }
    }

    private sealed class ResultObserver : IObserver<IOperationResult<string>>
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(500);

        private readonly SemaphoreSlim _resultSemaphore = new(0);
        private readonly Queue<IOperationResult<string>> _results = new();

        public async Task<IOperationResult<string>> WaitForResult()
        {
            if (!await _resultSemaphore.WaitAsync(_timeout))
            {
                throw new TimeoutException($"Did not receive a result in {_timeout}");
            }

            return _results.Dequeue();
        }

        public void OnNext(IOperationResult<string> value)
        {
            _results.Enqueue(value);
            _resultSemaphore.Release();
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
