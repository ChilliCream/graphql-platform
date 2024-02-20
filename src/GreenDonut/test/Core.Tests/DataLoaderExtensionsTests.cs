// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut;

public class DataLoaderExtensionsTests
{
    [Fact(DisplayName = "Set: Should throw an argument null exception for dataLoader")]
    public void SetDataLoaderNull()
    {
        // arrange
        var key = "Foo";
        var value = "Bar";

        // act
        void Verify() => default(IDataLoader<string, string>)!.Set(key, value);

        // assert
        Assert.Throws<ArgumentNullException>("dataLoader", Verify);
    }

    [Fact(DisplayName = "Set: Should throw an argument null exception for key")]
    public void SetKeyNull()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = "Bar";

        // act
        void Verify() => loader.Set(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "Set: Should not throw any exception")]
    public void SetNoException()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.Set(key, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact(DisplayName = "Set: Should result in a new cache entry")]
    public async Task SetNewCacheEntry()
    {
        // arrange
        using var cacheOwner = new TaskCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch, 
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache,
                CancellationToken = cacheOwner.CancellationToken,
            });
        
        const string key = "Foo";
        const string  value = "Bar";

        // act
        loader.Set(key, value);

        // assert
        var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        Assert.Equal(value, loadResult);
    }

    [Fact(DisplayName = "Set: Should result in 'Bar'")]
    public async Task SetTwice()
    {
        // arrange
        using var cacheOwner = new TaskCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch, 
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache,
                CancellationToken = cacheOwner.CancellationToken,
            });
        
        const string key = "Foo";
        const string first = "Bar";
        const string second = "Baz";

        // act
        loader.Set(key, first);
        loader.Set(key, second);

        // assert
        var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        Assert.Equal(first, loadResult);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for dataLoader")]
    public void IDataLoaderSetDataLoaderNull()
    {
        // arrange
        object key = "Foo";
        object value = "Bar";

        // act
        void Verify() => default(IDataLoader)!.Set(key, value);

        // assert
        Assert.Throws<ArgumentNullException>("dataLoader", Verify);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for key")]
    public void IDataLoaderSetKeyNull()
    {
        // arrange
        using var cacheOwner = new TaskCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch, 
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache,
                CancellationToken = cacheOwner.CancellationToken,
            });
        object value = "Bar";

        // act
        void Verify() => loader.Set(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should not throw any exception")]
    public void IDataLoaderSetNoException()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        void Verify() => loader.Set(key, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }
}
