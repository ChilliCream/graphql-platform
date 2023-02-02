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
        var dataLoader = new EmptyBatchDataLoader(new AutoBatchScheduler());

        // act
        var result = await dataLoader.LoadAsync("1");

        // assert
        Assert.Null(result);
    }

    public class EmptyBatchDataLoader : BatchDataLoader<string, string>
    {
        public EmptyBatchDataLoader(IBatchScheduler batchScheduler)
            : base(batchScheduler)
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
        var dataLoader = new CustomBatchDataLoader(
            new DataLoaderOptions());

        // act
        var result = await dataLoader.LoadAsync("abc");

        // assert
        Assert.Equal("Value:abc", result);
    }

    public class CustomBatchDataLoader : CacheDataLoader<string, string>
    {
        public CustomBatchDataLoader(DataLoaderOptions options)
            : base(options)
        {
        }

        protected override Task<string> LoadSingleAsync(
            string key,
            CancellationToken cancellationToken)
            => Task.FromResult("Value:" + key);
    }
}
