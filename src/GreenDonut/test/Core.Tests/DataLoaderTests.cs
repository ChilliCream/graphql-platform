using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using static GreenDonut.TestHelpers;
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable InconsistentNaming

namespace GreenDonut;

public class DataLoaderTests(ITestOutputHelper output)
{
    [Fact(DisplayName = "ClearCache: Should not throw any exception")]
    public void ClearCacheNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var services = new ServiceCollection()
            .AddScoped<IBatchScheduler, ManualBatchScheduler>()
            .AddDataLoader(sp => new DataLoader<string, string>(fetch, sp.GetRequiredService<IBatchScheduler>()));
        var scope = services.BuildServiceProvider().CreateScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<DataLoader<string, string>>();

        // act
        void Verify() => dataLoader.ClearCache();

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "ClearCache: Should remove all entries from the cache")]
    public void ClearCacheAllEntries()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);

        loader.SetCacheEntry("Foo", Task.FromResult<string?>("Bar"));
        loader.SetCacheEntry("Bar", Task.FromResult<string?>("Baz"));

        // act
        loader.ClearCache();

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for key")]
    public async Task LoadSingleKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<string?> Verify() => loader.LoadAsync(default(string)!, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "LoadAsync: Should match snapshot")]
    public async Task LoadSingleResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        var loadResult = loader.LoadAsync(key);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should match snapshot when same key is load twice")]
    public async Task LoadSingleResultTwice()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new DelayDispatcher();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // first load.
        (await loader.LoadAsync(key)).MatchSnapshot();

        // act
        var result = await loader.LoadAsync(key);

        // assert
        result.MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should match snapshot when using no cache")]
    public async Task LoadSingleResultNoCache()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler);
        var key = "Foo";

        // act
        var loadResult = loader.LoadAsync(key);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should return one error")]
    public async Task LoadSingleErrorResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        Task<string?> Verify() => loader.LoadAsync(key, CancellationToken.None);

        // assert
        var task = Assert.ThrowsAsync<InvalidOperationException>(Verify);
        await Task.Delay(25);
        batchScheduler.Dispatch();

        await task;
    }

    [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
    public async Task LoadParamsKeysNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<IReadOnlyList<string?>> Verify() => loader.LoadAsync(default(string[])!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("keys", Verify);
    }

    [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
    public async Task LoadParamsZeroKeys()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = Array.Empty<string>();

        // act
        var loadResult = loader.LoadAsync(keys);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        Assert.Empty(await loadResult);
    }

    [Fact(DisplayName = "LoadAsync: Should match snapshot")]
    public async Task LoadParamsResult()
    {
        // arrange
        var fetch = TestHelpers
            .CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new[] { "Foo", };

        // act
        var loadResult = loader.LoadAsync(keys);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
    public async Task LoadCollectionKeysNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<IReadOnlyList<string?>> Verify()
            => loader.LoadAsync(default(List<string>)!, CancellationToken.None);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("keys", Verify);
    }

    [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
    public async Task LoadCollectionZeroKeys()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new List<string>();

        // act
        var loadResult = loader.LoadAsync(keys, CancellationToken.None);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        Assert.Empty(await loadResult);
    }

    [Fact(DisplayName = "LoadAsync: Should return one result")]
    public async Task LoadCollectionResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new List<string> { "Foo", };

        // act
        var loadResult = loader.LoadAsync(keys, CancellationToken.None);
        batchScheduler.Dispatch();

        // assert
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should match snapshot if same key is fetched twice")]
    public async Task LoadCollectionResultTwice()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new DelayDispatcher();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler);
        var keys = new List<string> { "Foo", };

        (await loader.LoadAsync(keys, CancellationToken.None)).MatchSnapshot();

        // act
        var result = await loader.LoadAsync(keys, CancellationToken.None);

        // assert
        result.MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should return one result when cache is deactivated")]
    public async Task LoadCollectionResultNoCache()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(
            fetch,
            batchScheduler);
        var keys = new List<string> { "Foo", };

        // act
        var loadResult = loader.LoadAsync(keys, CancellationToken.None);
        batchScheduler.Dispatch();

        // assert
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "LoadAsync: Should return a list with null values")]
    public async Task LoadWithNullValues()
    {
        // arrange
        var repository = new Dictionary<string, string?>
        {
            { "Foo", "Bar" },
            { "Bar", null },
            { "Baz", "Foo" },
            { "Qux", null },
        };

        ValueTask Fetch(
            IReadOnlyList<string> keys,
            Memory<Result<string?>> results,
            CancellationToken cancellationToken)
        {
            var span = results.Span;

            for (var i = 0; i < keys.Count; i++)
            {
                if (repository.TryGetValue(keys[i], out var result))
                {
                    span[i] = result;
                }
            }

            return default;
        }

        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string?>(Fetch, batchScheduler);
        var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux", };

        // act
        var loadResult = loader.LoadAsync(requestKeys);
        batchScheduler.Dispatch();

        // assert
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName =
        "LoadAsync: Should result in a list of error results and cleaning up the " +
        "cache because the key and value list count are not equal")]
    public async Task LoadKeyAndValueCountNotEqual()
    {
        // arrange
        var expectedException = Errors.CreateKeysAndValuesMustMatch(4, 3);

        var repository = new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "Bar", "Baz" },
            { "Baz", "Foo" },
        };

        ValueTask Fetch(
            IReadOnlyList<string> keys,
            Memory<Result<string?>> results,
            CancellationToken cancellationToken)
        {
            var span = results.Span;

            for (var i = 0; i < keys.Count; i++)
            {
                if (repository.TryGetValue(keys[i], out var result))
                {
                    span[i] = result;
                }
            }

            return default;
        }

        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(Fetch, batchScheduler);
        var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux", };

        // act
        Task Verify() => loader.LoadAsync(requestKeys);

        // assert
        var task =
            Assert.ThrowsAsync<InvalidOperationException>(Verify);

        batchScheduler.Dispatch();

        var actualException = await task;

        Assert.Equal(expectedException.Message, actualException.Message);
    }

    [Fact(DisplayName = "LoadAsync: Should handle batching error")]
    public async Task LoadBatchingError()
    {
        // arrange
        var expectedException = new Exception("Foo");
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(Fetch, batchScheduler);
        var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux", };

        ValueTask Fetch(
            IReadOnlyList<string> keys,
            Memory<Result<string?>> results,
            CancellationToken cancellationToken)
            => throw expectedException;

        // act
        Task Verify() => loader.LoadAsync(requestKeys);

        // assert
        var task = Assert.ThrowsAsync<Exception>(Verify);

        batchScheduler.Dispatch();

        var actualException = await task;

        Assert.Equal(expectedException, actualException);
    }

    [InlineData(5, 25, 25, 1, true, true)]
    [InlineData(5, 25, 25, 0, true, true)]
    [InlineData(5, 25, 25, 0, true, false)]
    [InlineData(5, 25, 25, 0, false, true)]
    [InlineData(5, 25, 25, 0, false, false)]
    [InlineData(100, 1000, 25, 25, true, true)]
    [InlineData(100, 1000, 25, 0, true, true)]
    [InlineData(100, 1000, 25, 0, true, false)]
    [InlineData(100, 1000, 25, 25, false, true)]
    [InlineData(100, 1000, 25, 0, false, false)]
    [Theory(DisplayName = "LoadAsync: Runs integration tests with different settings")]
    public async Task LoadTest(
        int uniqueKeys,
        int maxRequests,
        int maxDelay,
        int maxBatchSize,
        bool caching,
        bool batching)
    {
        // arrange
        var random = new Random();

        ValueTask Fetch(
            IReadOnlyList<Guid> keys,
            Memory<Result<int>> results,
            CancellationToken cancellationToken)
        {
            for (var index = 0; index < keys.Count; index++)
            {
                var value = random.Next(1, maxRequests);
                results.Span[index] = value;
            }

            return Wait();

            async ValueTask Wait()
                => await Task.Delay(random.Next(maxDelay), cancellationToken);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = cts.Token;
        using var cacheOwner = caching
            ? new PromiseCacheOwner()
            : null;

        var options = new DataLoaderOptions
        {
            Cache = cacheOwner?.Cache,
            MaxBatchSize = batching ? 1 : maxBatchSize,
        };

        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<Guid, int>(Fetch, batchScheduler, options);
        var keyArray = new Guid[uniqueKeys];

        for (var i = 0; i < keyArray.Length; i++)
        {
            keyArray[i] = Guid.NewGuid();
        }

        var requests = new Task<int>[maxRequests];

        // act
        output.WriteLine("LoadAsync");
        for (var i = 0; i < maxRequests; i++)
        {
            requests[i] = Task.Factory.StartNew(async () =>
            {
                var index = random.Next(uniqueKeys);
                var delay = random.Next(maxDelay);

                await Task.Delay(delay, ct);

                return await loader.LoadAsync(keyArray[index], ct);
            }, TaskCreationOptions.RunContinuationsAsynchronously).Unwrap();
        }

        output.WriteLine("Start Dispatch");
        while (requests.Any(task => !task.IsCompleted))
        {
            output.WriteLine("Wait");
            await Task.Delay(25, ct);
            output.WriteLine("Dispatch");
            batchScheduler.Dispatch();
        }

        // assert
        output.WriteLine("Wait for results.");
        var responses = await Task.WhenAll(requests);

        foreach (var response in responses)
        {
            Assert.True(response > 0);
        }
    }

    [Fact(DisplayName = "RemoveCacheEntry: Should throw an argument null exception for key")]
    public void RemoveCacheEntryKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        loader.SetCacheEntry("Foo", Task.FromResult<string?>("Bar"));

        // act
        void Verify() => loader.RemoveCacheEntry(default!);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "RemoveCacheEntry: Should not throw any exception")]
    public void RemoveCacheEntryNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.RemoveCacheEntry(key);

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "RemoveCacheEntry: Should remove an existing entry")]
    public void RemoveCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";

        loader.SetCacheEntry(key, Task.FromResult<string?>("Bar"));

        // act
        loader.RemoveCacheEntry(key);

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "SetCacheEntry: Should throw an argument null exception for key")]
    public void SetCacheEntryKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = Task.FromResult<string?>("Foo");

        // act
        void Verify() => loader.SetCacheEntry(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "SetCacheEntry: Should throw an argument null exception for value")]
    public void SetCacheEntryValueNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.SetCacheEntry(key, default!);

        // assert
        Assert.Throws<ArgumentNullException>("value", Verify);
    }

    [Fact(DisplayName = "SetCacheEntry: Should result in a new cache entry")]
    public void SetCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";
        var value = Task.FromResult<string?>("Bar");

        // act
        loader.SetCacheEntry(key, value);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact(DisplayName = "SetCacheEntry: Should result in 'Bar'")]
    public void SetCacheEntryTwice()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";
        var first = Task.FromResult<string?>("Bar");
        var second = Task.FromResult<string?>("Baz");

        // act
        loader.SetCacheEntry(key, first);
        loader.SetCacheEntry(key, second);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for key")]
    public async Task IDataLoaderLoadSingleKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<object?> Verify() => loader.LoadAsync(default(object)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
    public async Task IDataLoaderLoadSingleResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        var loadResult = loader.LoadAsync(key);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one error")]
    public async Task IDataLoaderLoadSingleErrorResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        Task<object?> Verify() => loader.LoadAsync(key);

        // assert
        var task =
            Assert.ThrowsAsync<InvalidOperationException>(Verify);

        await Task.Delay(25);
        batchScheduler.Dispatch();

        await task;
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
    public async Task IDataLoaderLoadParamsKeysNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<IReadOnlyList<object?>> Verify() => loader.LoadAsync(default(object[])!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("keys", Verify);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
    public async Task IDataLoaderLoadParamsZeroKeys()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = Array.Empty<object>();

        // act
        var loadResult = await loader.LoadAsync(keys);

        // assert
        Assert.Empty(loadResult);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
    public async Task IDataLoaderLoadParamsResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new object[] { "Foo", };

        // act
        var loadResult = loader.LoadAsync(keys);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
    public async Task IDataLoaderLoadCollectionKeysNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<IReadOnlyList<object?>> Verify()
            => loader.LoadAsync(default(List<object>)!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("keys", Verify);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
    public async Task IDataLoaderLoadCollectionZeroKeys()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new List<object>();

        // act
        var loadResult = await loader.LoadAsync(keys);

        // assert
        Assert.Empty(loadResult);
    }

    [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
    public async Task IDataLoaderLoadCollectionResult()
    {
        // arrange
        var fetch = CreateFetch<string, string>("Bar");
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var keys = new List<object> { "Foo", };

        // act
        var loadResult = loader.LoadAsync(keys);

        // assert
        await Task.Delay(25);
        batchScheduler.Dispatch();
        (await loadResult).MatchSnapshot();
    }

    [Fact(DisplayName = "IDataLoader.RemoveCacheEntry: Should throw an argument null exception for key")]
    public void IDataLoaderRemoveCacheEntryKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);

        loader.SetCacheEntry("Foo", Task.FromResult((object?)"Bar"));

        // act
        void Verify() => loader.RemoveCacheEntry(null!);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.RemoveCacheEntry: Should not throw any exception")]
    public void IDataLoaderRemoveCacheEntryNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        void Verify() => loader.RemoveCacheEntry(key);

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "IDataLoader.RemoveCacheEntry: Should remove an existing entry")]
    public void IDataLoaderRemoveCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        object key = "Foo";

        loader.SetCacheEntry(key, Task.FromResult((object?)"Bar"));

        // act
        loader.RemoveCacheEntry(key);

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should throw an argument null exception for key")]
    public void IDataLoaderSetCacheEntryKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = Task.FromResult<object?>("Foo");

        // act
        void Verify() => loader.SetCacheEntry(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should throw an argument null exception for value")]
    public void IDataLoaderSetCacheEntryValueNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        void Verify() => loader.SetCacheEntry(key, default!);

        // assert
        Assert.Throws<ArgumentNullException>("value", Verify);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should not throw any exception")]
    public void IDataLoaderSetCacheEntryNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";
        var value = Task.FromResult<object?>("Bar");

        // act
        void Verify() => loader.SetCacheEntry(key, value);

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should result in a new cache entry")]
    public void IDataLoaderSetCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        object key = "Foo";
        var value = Task.FromResult<object?>("Bar");

        // act
        loader.SetCacheEntry(key, value);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact(DisplayName = "IDataLoader.SetCacheEntry: Should result in 'Bar'")]
    public void IDataLoaderSetCacheEntryTwice()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new PromiseCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        const string key = "Foo";
        var first = Task.FromResult((object?)"Bar");
        var second = Task.FromResult((object?)"Baz");

        // act
        loader.SetCacheEntry(key, first);
        loader.SetCacheEntry(key, second);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact]
    public async Task Add_Additional_Lookup_With_CacheObserver()
    {
        // arrange
        var cache = new PromiseCache(10);

        var dataLoader1 = new TestDataLoader1(
            new AutoBatchScheduler(),
            new DataLoaderOptions { Cache = cache });
        var entity1 = await dataLoader1.LoadAsync(1, CancellationToken.None);
        await Task.Delay(500);

        // act
        var dataLoader2 = new TestDataLoader2(
            new AutoBatchScheduler(),
            new DataLoaderOptions { Cache = cache });
        var entity2 = await dataLoader2.LoadAsync(2, CancellationToken.None);

        // assert
        Assert.Same(entity1, entity2);
    }

    private class TestDataLoader1(
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : DataLoaderBase<int, Entity>(batchScheduler, options)
    {
        protected internal override ValueTask FetchAsync(
            IReadOnlyList<int> keys,
            Memory<Result<Entity?>> results,
            DataLoaderFetchContext<Entity> context,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                results.Span[i] = new Entity { Id = key, OtherId = key + 1 };
            }

            return default;
        }
    }

    private class TestDataLoader2 : DataLoaderBase<int, Entity>
    {
        public TestDataLoader2(
            IBatchScheduler batchScheduler,
            DataLoaderOptions? options = null)
            : base(batchScheduler, options)
        {
            PromiseCacheObserver
                .Create(value => value.OtherId, this)
                .Accept(this);
        }

        protected internal override ValueTask FetchAsync(
            IReadOnlyList<int> keys,
            Memory<Result<Entity?>> results,
            DataLoaderFetchContext<Entity> context,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                results.Span[i] = new Entity { Id = key + 1, OtherId = key };
            }

            return default;
        }
    }

    public class Entity
    {
        public int Id { get; set; }

        public int OtherId { get; set; }
    }
}
