using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace GreenDonut
{
    public class DataLoaderTests
    {
        [Fact(DisplayName = "Clear: Should not throw any exception")]
        public void ClearNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.Clear();

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Clear: Should remove all entries from the cache")]
        public void ClearAllEntries()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            var loader = new DataLoader<string, string>(batchScheduler, fetch, options);

            loader.Set("Foo", Task.FromResult("Bar"));
            loader.Set("Bar", Task.FromResult("Baz"));

            // act
            loader.Clear();

            // assert
            Assert.Equal(0, cache.Usage);
        }

        [Fact(DisplayName = "Dispose: Should dispose and not throw any exception")]
        public void DisposeNoExceptionNobatchingAndCaching()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.Dispose();

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for key")]
        public async Task LoadSingleKeyNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            string key = null;

            // act
            Func<Task<string>> verify = () => loader.LoadAsync(key);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should match snapshot")]
        public async Task LoadSingleResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";

            // act
            Func<Task<string>> verify = () => loader.LoadAsync(key);

            // assert
            Task<InvalidOperationException> task = Assert
                .ThrowsAsync<InvalidOperationException>(verify);

            await Task.Delay(25);
            batchScheduler.Dispatch();

            await task;
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
        public async Task LoadParamsKeysNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            string[] keys = null;

            // act
            Func<Task<IReadOnlyList<string>>> verify = () => loader.LoadAsync(keys);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("keys", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
        public async Task LoadParamsZeroKeys()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new string[0];

            // act
            Task<IReadOnlyList<string>> loadResult = loader.LoadAsync(keys);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            Assert.Empty(await loadResult);
        }

        [Fact(DisplayName = "LoadAsync: Should match snapshot")]
        public async Task LoadParamsResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new string[] {"Foo"};

            // act
            Task<IReadOnlyList<string>> loadResult = loader.LoadAsync(keys);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
        public async Task LoadCollectionKeysNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            List<string> keys = null;

            // act
            Func<Task<IReadOnlyList<string>>> verify = () => loader.LoadAsync(keys);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("keys", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
        public async Task LoadCollectionZeroKeys()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new List<string>();

            // act
            Task<IReadOnlyList<string>> loadResult = loader.LoadAsync(keys);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            Assert.Empty(await loadResult);
        }

        [Fact(DisplayName = "LoadAsync: Should return one result")]
        public async Task LoadCollectionResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new List<string> { "Foo" };

            // act
            Task<IReadOnlyList<string>> loadResult = loader.LoadAsync(keys);
            batchScheduler.Dispatch();

            // assert
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "LoadAsync: Should return a list with null values")]
        public async Task LoadWithNullValues()
        {
            // arrange
            Result<string> expectedResult = "Bar";
            var repository = new Dictionary<string, string>
            {
                { "Foo", "Bar" },
                { "Bar", null },
                { "Baz", "Foo" },
                { "Qux", null }
            };
            FetchDataDelegate<string, string> fetch = async (keys, cancellationToken) =>
            {
                var values = new List<Result<string>>();

                foreach (var key in keys)
                {
                    if (repository.ContainsKey(key))
                    {
                        values.Add(repository[key]);
                    }
                }

                return await Task.FromResult(values);
            };
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux" };

            // act
            Task<IReadOnlyList<string>> loadResult = loader.LoadAsync(requestKeys);
            batchScheduler.Dispatch();

            // assert
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "LoadAsync: Should result in a list of error results and cleaning up the cache because the key and value list count are not equal", Skip = "FIx this Test")]
        public async Task LoadKeyAndValueCountNotEquel()
        {
            // arrange
            InvalidOperationException expectedException = Errors
                .CreateKeysAndValuesMustMatch(4, 3);
            Result<string> expectedResult = "Bar";
            var repository = new Dictionary<string, string>
            {
                { "Foo", "Bar" },
                { "Bar", "Baz" },
                { "Baz", "Foo" }
            };
            FetchDataDelegate<string, string> fetch =
                async (keys, cancellationToken) =>
                {
                    var values = new List<Result<string>>();

                    foreach (var key in keys)
                    {
                        if (repository.ContainsKey(key))
                        {
                            values.Add(repository[key]);
                        }
                    }

                    return await Task.FromResult(values);
                };
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var requestKeys = new [] { "Foo", "Bar", "Baz", "Qux" };

            // act
            Func<Task> verify = () => loader.LoadAsync(requestKeys);

            // assert
            Task<InvalidOperationException> task = Assert
                .ThrowsAsync<InvalidOperationException>(verify);

            batchScheduler.Dispatch();

            InvalidOperationException actualException = await task;

            Assert.Equal(expectedException.Message, actualException.Message);
        }

        [Fact(DisplayName = "LoadAsync: Should handle batching error")]
        public async Task LoadBatchingError()
        {
            // arrange
            var expectedException = new Exception("Foo");
            Result<string> expectedResult = "Bar";
            FetchDataDelegate<string, string> fetch = (keys, cancellationToken) =>
                throw expectedException;
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux" };

            // act
            Func<Task> verify = () => loader.LoadAsync(requestKeys);

            // assert
            Task<Exception> task = Assert.ThrowsAsync<Exception>(verify);

            batchScheduler.Dispatch();

            Exception actualException = await task;

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
        [InlineData(100, 1000, 25, 0, false, true)]
        [InlineData(100, 1000, 25, 0, false, false)]
        [Theory(DisplayName = "LoadAsync: Runs integration tests with different settings")]
        public async Task LoadTest(int uniqueKeys, int maxRequests, int maxDelay, int maxBatchSize,
            bool caching, bool batching)
        {
            // arrange
            var random = new Random();
            FetchDataDelegate<Guid, int> fetch =
                async (keys, cancellationToken) =>
                {
                    var values = new List<Result<int>>(keys.Count);

                    foreach (Guid key in keys)
                    {
                        var value = random.Next(1, maxRequests);

                        values.Add(value);
                    }

                    var delay = random.Next(maxDelay);

                    await Task.Delay(delay);

                    return values;
                };
            var options = new DataLoaderOptions<Guid>
            {
                Caching = caching,
                Batch = batching,
                MaxBatchSize = maxBatchSize
            };
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<Guid, int>(batchScheduler, fetch, options);
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
                }, TaskCreationOptions.RunContinuationsAsynchronously)
                    .Unwrap();
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            string key = null;

            loader.Set("Foo", Task.FromResult("Bar"));

            // act
            Action verify = () => loader.Remove(key);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "Remove: Should not throw any exception")]
        public void RemoveNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";

            // act
            Action verify = () => loader.Remove(key);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Remove: Should remove an existing entry")]
        public void RemoveEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            var loader = new DataLoader<string, string>(batchScheduler, fetch, options);
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            string key = null;
            var value = Task.FromResult("Foo");

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "Set: Should throw an argument null exception for value")]
        public void SetValueNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";
            Task<string> value = null;

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("value", verify);
        }

        [Fact(DisplayName = "Set: Should result in a new cache entry")]
        public void SetNewCacheEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            var loader = new DataLoader<string, string>(batchScheduler, fetch, options);
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            var loader = new DataLoader<string, string>(batchScheduler, fetch, options);
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = null;

            // act
            Func<Task<object>> verify = () => loader.LoadAsync(key);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        public async Task IDataLoaderLoadSingleResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";

            // act
            Task<object> loadResult = loader.LoadAsync(key);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one error")]
        public async Task IDataLoaderLoadSingleErrorResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";

            // act
            Func<Task<object>> verify = () => loader.LoadAsync(key);

            // assert
            Task<InvalidOperationException> task = Assert
                .ThrowsAsync<InvalidOperationException>(verify);

            await Task.Delay(25);
            batchScheduler.Dispatch();

            await task;
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
        public async Task IDataLoaderLoadParamsKeysNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object[] keys = null;

            // act
            Func<Task<IReadOnlyList<object>>> verify = () => loader.LoadAsync(keys);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("keys", verify);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
        public async Task IDataLoaderLoadParamsZeroKeys()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new object[0];

            // act
            IReadOnlyList<object> loadResult = await loader.LoadAsync(keys);

            // assert
            Assert.Empty(loadResult);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        public async Task IDataLoaderLoadParamsResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new object[] { "Foo" };

            // act
            Task<IReadOnlyList<object>> loadResult = loader.LoadAsync(keys);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
        public async Task IDataLoaderLoadCollectionKeysNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            List<object> keys = null;

            // act
            Func<Task<IReadOnlyList<object>>> verify = () => loader.LoadAsync(keys);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("keys", verify);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
        public async Task IDataLoaderLoadCollectionZeroKeys()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new List<object>();

            // act
            IReadOnlyList<object> loadResult = await loader.LoadAsync(keys);

            // assert
            Assert.Empty(loadResult);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        public async Task IDataLoaderLoadCollectionResult()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            var keys = new List<object> { "Foo" };

            // act
            Task<IReadOnlyList<object>> loadResult = loader.LoadAsync(keys);

            // assert
            await Task.Delay(25);
            batchScheduler.Dispatch();
            (await loadResult).MatchSnapshot();
        }

        [Fact(DisplayName = "IDataLoader.Remove: Should throw an argument null exception for key")]
        public void IDataLoaderRemoveKeyNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = null;

            loader.Set("Foo", Task.FromResult((object)"Bar"));

            // act
            Action verify = () => loader.Remove(key);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "IDataLoader.Remove: Should not throw any exception")]
        public void IDataLoaderRemoveNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";

            // act
            Action verify = () => loader.Remove(key);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.Remove: Should remove an existing entry")]
        public void IDataLoaderRemoveEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options  = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch, options);
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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = null;
            Task<object> value = Task.FromResult<object>("Foo");

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for value")]
        public void IDataLoaderSetValueNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";
            Task<object> value = null;

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("value", verify);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should not throw any exception")]
        public void IDataLoaderSetNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";
            Task<object> value = Task.FromResult<object>("Bar");

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.Set: Should result in a new cache entry")]
        public void IDataLoaderSetNewCacheEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options  = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch, options);
            object key = "Foo";
            Task<object> value = Task.FromResult<object>("Bar");

            // act
            loader.Set(key, value);

            // assert
            Assert.Equal(1, cache.Usage);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should result in 'Bar'")]
        public void IDataLoaderSetTwice()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var cache = new TaskCache(10);
            var options  = new DataLoaderOptions<string>
            {
                Cache = cache
            };
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch, options);
            var key = "Foo";
            var first = Task.FromResult((object)"Bar");
            var second = Task.FromResult((object)"Baz");

            // act
            loader.Set(key, first);
            loader.Set(key, second);

            // assert
            Assert.Equal(1, cache.Usage);
        }
    }
}
