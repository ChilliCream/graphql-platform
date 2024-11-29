using Xunit;

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

    public sealed class InstantDispatcher : IBatchScheduler
    {
        public void Schedule(Func<ValueTask> dispatch)
            => dispatch();
    }
}
