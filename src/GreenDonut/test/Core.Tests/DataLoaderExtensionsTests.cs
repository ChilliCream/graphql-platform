// ReSharper disable InconsistentNaming

using Xunit;

namespace GreenDonut;

public class DataLoaderExtensionsTests
{
    [Fact(DisplayName = "SetCacheEntry: Should throw an argument null exception for dataLoader")]
    public void SetCacheEntryDataLoaderNull()
    {
        // arrange
        var key = "Foo";
        var value = "Bar";

        // act
        void Verify() => default(IDataLoader<string, string>)!.SetCacheEntry(key, value);

        // assert
        Assert.Throws<ArgumentNullException>("dataLoader", Verify);
    }

    [Fact(DisplayName = "SetCacheEntry: Should throw an argument null exception for key")]
    public void SetCacheEntryKeyNull()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = "Bar";

        // act
        void Verify() => loader.SetCacheEntry(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "SetCacheEntry: Should not throw any exception")]
    public void SetCacheEntryNoException()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.SetCacheEntry(key, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact(DisplayName = "SetCacheEntry: Should result in a new cache entry")]
    public async Task SetCacheEntry()
    {
        // arrange
        using var cacheOwner = new PromiseCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache
            });

        const string key = "Foo";
        const string  value = "Bar";

        // act
        loader.SetCacheEntry(key, value);

        // assert
        var loadResult = await loader.LoadAsync(key);

        Assert.Equal(value, loadResult);
    }

    [Fact(DisplayName = "SetCacheEntry: Should result in 'Bar'")]
    public async Task SetCacheEntryTwice()
    {
        // arrange
        using var cacheOwner = new PromiseCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache
            });

        const string key = "Foo";
        const string first = "Bar";
        const string second = "Baz";

        // act
        loader.SetCacheEntry(key, first);
        loader.SetCacheEntry(key, second);

        // assert
        var loadResult = await loader.LoadAsync(key);

        Assert.Equal(first, loadResult);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should throw an argument null exception for dataLoader")]
    public void IDataLoaderSetCacheEntryDataLoaderNull()
    {
        // arrange
        object key = "Foo";
        object value = "Bar";

        // act
        void Verify() => default(IDataLoader)!.SetCacheEntry(key, value);

        // assert
        Assert.Throws<ArgumentNullException>("dataLoader", Verify);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should throw an argument null exception for key")]
    public void IDataLoaderSetCacheEntryKeyNull()
    {
        // arrange
        using var cacheOwner = new PromiseCacheOwner();
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler,
            new DataLoaderOptions
            {
                Cache = cacheOwner.Cache
            });
        object value = "Bar";

        // act
        void Verify() => loader.SetCacheEntry(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should not throw any exception")]
    public void IDataLoaderSetCacheEntryNoException()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        void Verify() => loader.SetCacheEntry(key, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Verify);
    }
}
