using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task Null_Result()
    {
        // arrange
        using var cacheOwner = new TaskCacheOwner();
        var dataLoader = new EmptyBatchDataLoader(
            new AutoBatchScheduler(),
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache, 
                CancellationToken = cacheOwner.CancellationToken,
            });

        // act
        var result = await dataLoader.LoadAsync("1");

        // assert
        Assert.Null(result);
    }

    public class EmptyBatchDataLoader : BatchDataLoader<string, string>
    {
        public EmptyBatchDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
            : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());
    }

    public class CustomBatchDataLoader : BatchDataLoader<string, string>
    {
        public CustomBatchDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(
                keys.ToDictionary(t => t, t => "Value:" + t));
    }
}

public class CacheDataLoaderTests
{
    [Fact]
    public async Task LoadSingleAsync()
    {
        // arrange
        using var cacheOwner = new TaskCacheOwner();
        var dataLoader = new CustomCacheDataLoader(
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache,
                CancellationToken = cacheOwner.CancellationToken,
            });

        // act
        var result = await dataLoader.LoadAsync("abc");

        // assert
        Assert.Equal("Value:abc", result);
    }

    public class CustomCacheDataLoader(DataLoaderOptions options)
        : CacheDataLoader<string, string>(options)
    {
        protected override Task<string> LoadSingleAsync(
            string key,
            CancellationToken cancellationToken)
            => Task.FromResult("Value:" + key);
    }
}
