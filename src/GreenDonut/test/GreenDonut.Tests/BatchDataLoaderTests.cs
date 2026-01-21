namespace GreenDonut;

public class BatchDataLoaderTests
{
    [Fact]
    public async Task LoadSingleAsync()
    {
        // arrange
        var dataLoader = new CustomBatchDataLoader(
            new AutoBatchScheduler(),
            new DataLoaderOptions());

        // act
        var result = await dataLoader.LoadAsync("abc");

        // assert
        Assert.Equal("Value:abc", result);
    }

    [Fact]
    public async Task LoadTwoAsync()
    {
        // arrange
        var dataLoader = new CustomBatchDataLoader(
            new DelayDispatcher(),
            new DataLoaderOptions());

        // act
        var result1 = dataLoader.LoadAsync("1abc");
        var result2 = dataLoader.LoadAsync("0abc");

        // assert
        Assert.Equal("Value:1abc", await result1);
        Assert.Equal("Value:0abc", await result2);
    }

    [Fact]
    public async Task LoadTheSameKeyTwiceWillYieldSamePromise()
    {
        // arrange
        var dataLoader = new CustomBatchDataLoader(
            new DelayDispatcher(),
            new DataLoaderOptions());

        // act
        var result1 = dataLoader.LoadAsync("1abc");
        var result2 = dataLoader.LoadAsync("1abc");

        // assert
        Assert.Same(result1, result2);
        Assert.Equal("Value:1abc", await result1);
        Assert.Equal("Value:1abc", await result2);
    }

    [Fact]
    public async Task LoadAsync_Should_BatchAllItemsOfList()
    {
        // arrange
        var cts = new CancellationTokenSource(5000);

        var dataLoader = new CustomBatchDataLoader(
            new InstantDispatcher(),
            new DataLoaderOptions());

        // act
        await dataLoader.LoadAsync(["1abc", "0abc"], cts.Token);

        // assert
        Assert.Equal(1, dataLoader.ExecutionCount);
    }

    [Fact]
    public async Task BatchDataLoader_MissingKey_Should_Be_ResultResolveDefault()
    {
        // arrange
        var loader = new TestBatchLoader();
        var keys = new[] { 1, 2 };
        var results = new Result<string?>[2];

        // act
        await loader.FetchAsync(keys, results, default, CancellationToken.None);

        Assert.Equal("one", results[0].Value);
        Assert.Equal(ResultKind.Value, results[0].Kind);

        // assert
        Assert.Null(results[1].Value);
        Assert.Equal(ResultKind.Value, results[1].Kind);
    }

    [Fact]
    public async Task StatefulBatchDataLoader_MissingKey_Should_Be_ResultResolveDefault()
    {
        // arrange
        var loader = new TestStatefulBatchLoader();
        var keys = new[] { 1, 2 };
        var results = new Result<string?>[2];

        // act
        await loader.FetchAsync(keys, results, default, CancellationToken.None);

        Assert.Equal("one", results[0].Value);
        Assert.Equal(ResultKind.Value, results[0].Kind);

        // assert
        Assert.Null(results[1].Value);
        Assert.Equal(ResultKind.Value, results[1].Kind);
    }

    [Fact]
    public async Task Null_Result()
    {
        // arrange
        using var cacheOwner = new PromiseCacheOwner();
        var dataLoader = new EmptyBatchDataLoader(
            new AutoBatchScheduler(),
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache
            });

        // act
        var result = await dataLoader.LoadAsync("1");

        // assert
        Assert.Null(result);
    }

    public class EmptyBatchDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());
    }

    public class CustomBatchDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : BatchDataLoader<string, string>(batchScheduler, options)
    {
        private int _executionCount;
        public int ExecutionCount => _executionCount;

        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                keys.ToDictionary(t => t, t => "Value:" + t));
        }
    }

    public class TestBatchLoader : BatchDataLoader<int, string>
    {
        public TestBatchLoader()
            : base(new TestBatchScheduler(), new DataLoaderOptions()) { }

        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<int, string>>(
                new Dictionary<int, string> { { 1, "one" } });
        }
    }

    public class TestStatefulBatchLoader : StatefulBatchDataLoader<int, string>
    {
        public TestStatefulBatchLoader()
            : base(new TestBatchScheduler(), new DataLoaderOptions()) { }

        protected override Task<IReadOnlyDictionary<int, string>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<string> context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<int, string>>(
                new Dictionary<int, string> { { 1, "one" } });
        }
    }

    public sealed class InstantDispatcher : IBatchScheduler
    {
        public void Schedule(Batch batch)
            => batch.DispatchAsync();
    }
}
