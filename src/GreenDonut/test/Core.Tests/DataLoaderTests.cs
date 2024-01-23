using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;
using static GreenDonut.TestHelpers;

namespace GreenDonut;

public class DataLoaderTests
{
    [Fact(DisplayName = "Clear: Should not throw any exception")]
    public void ClearNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        void Verify() => loader.Clear();

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "Clear: Should remove all entries from the cache")]
    public void ClearAllEntries()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);

        loader.Set("Foo", Task.FromResult("Bar"));
        loader.Set("Bar", Task.FromResult("Baz"));

        // act
        loader.Clear();

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "Dispose: Should dispose and not throw any exception")]
    public void DisposeNoExceptionNoBatchingAndCaching()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        void Verify() => loader.Dispose();

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for key")]
    public async Task LoadSingleKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        // act
        Task<string> Verify() => loader.LoadAsync(default(string)!, CancellationToken.None);

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
            batchScheduler,
            new DataLoaderOptions
            {
                Caching = false
            });
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
        Task<string> Verify() => loader.LoadAsync(key, CancellationToken.None);

        // assert
        var task = Assert
            .ThrowsAsync<InvalidOperationException>((Func<Task<string>>)Verify);

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
        Task<IReadOnlyList<string>> Verify() => loader.LoadAsync(default(string[])!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>("keys", (Func<Task<IReadOnlyList<string>>>)Verify);
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
        Task<IReadOnlyList<string>> Verify()
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
            batchScheduler,
            new DataLoaderOptions
            {
                Caching = false
            });
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
        var repository = new Dictionary<string, string>
        {
            { "Foo", "Bar" },
            { "Bar", null },
            { "Baz", "Foo" },
            { "Qux", null }
        };

        ValueTask Fetch(
            IReadOnlyList<string> keys,
            Memory<Result<string>> results,
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
            { "Baz", "Foo" }
        };

        ValueTask Fetch(
            IReadOnlyList<string> keys,
            Memory<Result<string>> results,
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
            Memory<Result<string>> results,
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
    // [InlineData(100, 1000, 25, 25, true, true)]
    // [InlineData(100, 1000, 25, 0, true, true)]
    // [InlineData(100, 1000, 25, 0, true, false)]
    // [InlineData(100, 1000, 25, 25, false, true)]
    // [InlineData(100, 1000, 25, 0, false, false)]
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

        var options = new DataLoaderOptions
        {
            Caching = caching,
            MaxBatchSize = batching ? 1 : maxBatchSize
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
        for (var i = 0; i < maxRequests; i++)
        {
            requests[i] = Task.Factory.StartNew(async () =>
            {
                var index = random.Next(uniqueKeys);
                var delay = random.Next(maxDelay);

                await Task.Delay(delay);

                return await loader.LoadAsync(keyArray[index]);
            }, TaskCreationOptions.RunContinuationsAsynchronously).Unwrap();
        }

        while (requests.Any(task => !task.IsCompleted))
        {
            await Task.Delay(25);
            batchScheduler.Dispatch();
        }

        // assert
        var responses = await Task.WhenAll(requests);

        foreach (var response in responses)
        {
            Assert.True(response > 0);
        }
    }

    [Fact(DisplayName = "Remove: Should throw an argument null exception for key")]
    public void RemoveKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);

        loader.Set("Foo", Task.FromResult("Bar"));

        // act
        void Verify() => loader.Remove(default!);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "Remove: Should not throw any exception")]
    public void RemoveNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.Remove(key);

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [Fact(DisplayName = "Remove: Should remove an existing entry")]
    public void RemoveEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";

        loader.Set(key, Task.FromResult("Bar"));

        // act
        loader.Remove(key);

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "Set: Should throw an argument null exception for key")]
    public void SetKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = Task.FromResult("Foo");

        // act
        void Verify() => loader.Set(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "Set: Should throw an argument null exception for value")]
    public void SetValueNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var loader = new DataLoader<string, string>(fetch, batchScheduler);
        var key = "Foo";

        // act
        void Verify() => loader.Set(key, default!);

        // assert
        Assert.Throws<ArgumentNullException>("value", Verify);
    }

    [Fact(DisplayName = "Set: Should result in a new cache entry")]
    public void SetNewCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";
        var value = Task.FromResult("Bar");

        // act
        loader.Set(key, value);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact(DisplayName = "Set: Should result in 'Bar'")]
    public void SetTwice()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        var loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        var key = "Foo";
        var first = Task.FromResult("Bar");
        var second = Task.FromResult("Baz");

        // act
        loader.Set(key, first);
        loader.Set(key, second);

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
        Task<object> Verify() => loader.LoadAsync(default(object)!);

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
        Task<object> Verify() => loader.LoadAsync(key);

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
        Task<IReadOnlyList<object>> Verify() => loader.LoadAsync(default(object[])!);

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
        Task<IReadOnlyList<object>> Verify()
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

    [Fact(DisplayName = "IDataLoader.Remove: Should throw an argument null exception for key")]
    public void IDataLoaderRemoveKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = null;

        loader.Set("Foo", Task.FromResult((object)"Bar"));

        // act
        var verify = () => loader.Remove(key);

        // assert
        Assert.Throws<ArgumentNullException>("key", verify);
    }

    [Fact(DisplayName = "IDataLoader.Remove: Should not throw any exception")]
    public void IDataLoaderRemoveNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        var verify = () => loader.Remove(key);

        // assert
        Assert.Null(Record.Exception(verify));
    }

    [Fact(DisplayName = "IDataLoader.Remove: Should remove an existing entry")]
    public void IDataLoaderRemoveEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        object key = "Foo";

        loader.Set(key, Task.FromResult((object)"Bar"));

        // act
        loader.Remove(key);

        // assert
        Assert.Equal(0, cache.Usage);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for key")]
    public void IDataLoaderSetKeyNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        var value = Task.FromResult<object>("Foo");

        // act
        void Verify() => loader.Set(null!, value);

        // assert
        Assert.Throws<ArgumentNullException>("key", Verify);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for value")]
    public void IDataLoaderSetValueNull()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";

        // act
        void Verify() => loader.Set(key, default!);

        // assert
        Assert.Throws<ArgumentNullException>("value", Verify);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should not throw any exception")]
    public void IDataLoaderSetNoException()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler);
        object key = "Foo";
        var value = Task.FromResult<object>("Bar");

        // act
        var verify = () => loader.Set(key, value);

        // assert
        Assert.Null(Record.Exception(verify));
    }

    [Fact(DisplayName = "IDataLoader.Set: Should result in a new cache entry")]
    public void IDataLoaderSetNewCacheEntry()
    {
        // arrange
        var fetch = CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        object key = "Foo";
        var value = Task.FromResult<object>("Bar");

        // act
        loader.Set(key, value);

        // assert
        Assert.Equal(1, cache.Usage);
    }

    [Fact(DisplayName = "IDataLoader.Set: Should result in 'Bar'")]
    public void IDataLoaderSetTwice()
    {
        // arrange
        var fetch = TestHelpers.CreateFetch<string, string>();
        var batchScheduler = new ManualBatchScheduler();
        var cache = new TaskCache(10);
        var options = new DataLoaderOptions { Cache = cache, };
        IDataLoader loader = new DataLoader<string, string>(fetch, batchScheduler, options);
        const string key = "Foo";
        var first = Task.FromResult((object)"Bar");
        var second = Task.FromResult((object)"Baz");

        // act
        loader.Set(key, first);
        loader.Set(key, second);

        // assert
        Assert.Equal(1, cache.Usage);
    }
}