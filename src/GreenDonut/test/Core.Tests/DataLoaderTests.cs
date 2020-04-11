using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut
{
    public class DataLoaderTests
    {
        #region Clear

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

        [Fact(DisplayName = "Clear: Should remove all entries from the cache", Skip ="kk")]
        public async Task ClearAllEntries()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);

            loader.Set("Foo", Task.FromResult("Bar"));
            loader.Set("Bar", Task.FromResult("Baz"));

            // act
            loader.Clear();

            // assert
            Func<Task> verify = () => loader.LoadAsync("Foo", "Bar");

            await Assert.ThrowsAsync<InvalidOperationException>(verify).ConfigureAwait(false);
        }

        #endregion

        #region Dispose

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

        #endregion

        // #region LoadAsync(string key)

        // [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for key")]
        // public async Task LoadSingleKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     string key = null;

        //     // act
        //     Func<Task<string>> verify = () => loader.LoadAsync(key);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("key", verify).ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        // public async Task LoadSingleNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";

        //     // act
        //     Func<Task<string>> verify = () => loader.LoadAsync(key);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify).ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "LoadAsync: Should return one result")]
        // public async Task LoadSingleResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";

        //     // act
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     // assert
        //     Assert.Equal(expectedResult.Value, loadResult);
        // }

        // [Fact(DisplayName = "LoadAsync: Should return one error")]
        // public async Task LoadSingleErrorResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";

        //     // act
        //     Func<Task> verify = () => loader.LoadAsync(key);

        //     // assert
        //     await Assert.ThrowsAsync<InvalidOperationException>(verify)
        //         .ConfigureAwait(false);
        // }

        // #endregion

        // #region LoadAsync(params string[] keys)

        // [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
        // public async Task LoadParamsKeysNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     string[] keys = null;

        //     // act
        //     Func<Task<IReadOnlyList<string>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("keys", verify)
        //         .ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
        // public async Task LoadParamsZeroKeys()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new string[0];

        //     // act
        //     IReadOnlyList<string> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Empty(loadResult);
        // }

        // [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        // public async Task LoadParamsNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new string[] { "Foo" };

        //     // act
        //     Func<Task<IReadOnlyList<string>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify)
        //         .ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "LoadAsync: Should return one result")]
        // public async Task LoadParamsResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new string[] { "Foo" };

        //     // act
        //     IReadOnlyList<string> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Collection(loadResult,
        //         r => Assert.Equal(expectedResult.Value, r));
        // }

        // #endregion

        // #region LoadAsync(IReadOnlyCollection<string> keys)

        // [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for keys")]
        // public async Task LoadCollectionKeysNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     List<string> keys = null;

        //     // act
        //     Func<Task<IReadOnlyList<string>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("keys", verify)
        //         .ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "LoadAsync: Should allow empty list of keys")]
        // public async Task LoadCollectionZeroKeys()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new List<string>();

        //     // act
        //     IReadOnlyList<string> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Empty(loadResult);
        // }

        // [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        // public async Task LoadCollectionNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new List<string> { "Foo" };

        //     // act
        //     Func<Task<IReadOnlyList<string>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify)
        //         .ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "LoadAsync: Should return one result")]
        // public async Task LoadCollectionResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var keys = new List<string> { "Foo" };

        //     // act
        //     IReadOnlyList<string> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Collection(loadResult,
        //         v => Assert.Equal(expectedResult.Value, v));
        // }

        // [Fact(DisplayName = "LoadAsync: Should return a list with null values")]
        // public async Task LoadWithNullValuesl()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     var repository = new Dictionary<string, string>
        //     {
        //         { "Foo", "Bar" },
        //         { "Bar", null },
        //         { "Baz", "Foo" },
        //         { "Qux", null }
        //     };
        //     FetchDataDelegate<string, string> fetch =
        //         async (keys, cancellationToken) =>
        //         {
        //             var values = new List<Result<string>>();

        //             foreach (var key in keys)
        //             {
        //                 if (repository.ContainsKey(key))
        //                 {
        //                     values.Add(repository[key]);
        //                 }
        //             }

        //             return await Task.FromResult(values).ConfigureAwait(false);
        //         };
        //     var options = new DataLoaderOptions<string>
        //     {
        //         AutoDispatching = true
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux" };

        //     // act
        //     IReadOnlyList<string> results = await loader
        //         .LoadAsync(requestKeys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Collection(results,
        //         v => Assert.Equal("Bar", v),
        //         v => Assert.Null(v),
        //         v => Assert.Equal("Foo", v),
        //         v => Assert.Null(v));
        // }

        // [Fact(DisplayName = "LoadAsync: Should result in a list of error results and cleaning up the cache because the key and value list count are not equal")]
        // public async Task LoadKeyAndValueCountNotEquel()
        // {
        //     // arrange
        //     InvalidOperationException expectedException = Errors
        //         .CreateKeysAndValuesMustMatch(4, 3);
        //     Result<string> expectedResult = "Bar";
        //     var repository = new Dictionary<string, string>
        //     {
        //         { "Foo", "Bar" },
        //         { "Bar", "Baz" },
        //         { "Baz", "Foo" }
        //     };
        //     FetchDataDelegate<string, string> fetch =
        //         async (keys, cancellationToken) =>
        //         {
        //             var values = new List<Result<string>>();

        //             foreach (var key in keys)
        //             {
        //                 if (repository.ContainsKey(key))
        //                 {
        //                     values.Add(repository[key]);
        //                 }
        //             }

        //             return await Task.FromResult(values).ConfigureAwait(false);
        //         };
        //     var options = new DataLoaderOptions<string>
        //     {
        //         AutoDispatching = true
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var requestKeys = new [] { "Foo", "Bar", "Baz", "Qux" };

        //     // act
        //     Func<Task> verify = () => loader.LoadAsync(requestKeys);

        //     // assert
        //     InvalidOperationException actualException = await Assert
        //         .ThrowsAsync<InvalidOperationException>(verify)
        //         .ConfigureAwait(false);
        //     Assert.Equal(expectedException.Message, actualException.Message);
        // }

        // [Fact(
        //     DisplayName = "LoadAsync: Should handle batching error",
        //     Skip = "This one is not stable and has to be fixed.")]
        // public async Task LoadBatchingError()
        // {
        //     // arrange
        //     var expectedException = new Exception("Foo");
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch =
        //         (keys, cancellationToken) => throw expectedException;
        //     var options = new DataLoaderOptions<string>
        //     {
        //         AutoDispatching = true
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var requestKeys = new[] { "Foo", "Bar", "Baz", "Qux" };

        //     // act
        //     Func<Task> verify = () => loader.LoadAsync(requestKeys);

        //     // assert
        //     Exception actualException = await Assert
        //         .ThrowsAsync<Exception>(verify)
        //         .ConfigureAwait(false);
        //     Assert.Equal(expectedException, actualException);
        // }

        // #endregion

        // #region LoadTest

        // [InlineData(5, 25, 25, 1, true, true)]
        // [InlineData(5, 25, 25, 0, true, true)]
        // [InlineData(5, 25, 25, 0, true, true)]
        // [InlineData(5, 25, 25, 0, true, false)]
        // [InlineData(5, 25, 25, 0, false, true)]
        // [InlineData(5, 25, 25, 0, false, false)]
        // [InlineData(100, 1000, 25, 25, true, true)]
        // [InlineData(100, 1000, 25, 0, true, true)]
        // [InlineData(100, 1000, 25, 0, true, true)]
        // [InlineData(100, 1000, 25, 0, true, false)]
        // [InlineData(100, 1000, 25, 0, false, true)]
        // [InlineData(100, 1000, 25, 0, false, false)]
        // [Theory(DisplayName = "LoadAsync: Runs integration tests with different settings")]
        // public async Task LoadTest(int uniqueKeys, int maxRequests,
        //     int maxDelay, int maxBatchSize, bool caching, bool batching)
        // {
        //     // arrange
        //     var random = new Random();
        //     FetchDataDelegate<Guid, int> fetch =
        //         async (keys, cancellationToken) =>
        //         {
        //             var values = new List<Result<int>>(keys.Count);

        //             foreach (Guid key in keys)
        //             {
        //                 var value = random.Next(1, maxRequests);

        //                 values.Add(value);
        //             }

        //             var delay = random.Next(maxDelay);

        //             await Task.Delay(delay).ConfigureAwait(false);

        //             return values;
        //         };
        //     var options = new DataLoaderOptions<Guid>
        //     {
        //         AutoDispatching = true,
        //         Caching = caching,
        //         Batch = batching,
        //         MaxBatchSize = maxBatchSize
        //     };
        //     var dataLoader = new DataLoader<Guid, int>(options, fetch);
        //     var keyArray = new Guid[uniqueKeys];

        //     for (var i = 0; i < keyArray.Length; i++)
        //     {
        //         keyArray[i] = Guid.NewGuid();
        //     }

        //     var requests = new Task<int>[maxRequests];

        //     // act
        //     for (var i = 0; i < maxRequests; i++)
        //     {
        //         requests[i] = Task.Factory.StartNew(async () =>
        //         {
        //             var index = random.Next(uniqueKeys);
        //             var delay = random.Next(maxDelay);

        //             await Task.Delay(delay).ConfigureAwait(false);

        //             return await dataLoader.LoadAsync(keyArray[index])
        //                 .ConfigureAwait(false);
        //         }, TaskCreationOptions.RunContinuationsAsynchronously)
        //             .Unwrap();
        //     }

        //     // assert
        //     var responses = await Task.WhenAll(requests).ConfigureAwait(false);

        //     foreach (var response in responses)
        //     {
        //         Assert.True(response > 0);
        //     }
        // }

        // #endregion

        // #region Remove

        // [Fact(DisplayName = "Remove: Should throw an argument null exception for key")]
        // public void RemoveKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     string key = null;

        //     loader.Set("Foo", Task.FromResult("Bar"));

        //     // act
        //     Action verify = () => loader.Remove(key);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("key", verify);
        // }

        // [Fact(DisplayName = "Remove: Should not throw any exception")]
        // public void RemoveNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";

        //     // act
        //     Action verify = () => loader.Remove(key);

        //     // assert
        //     Assert.Null(Record.Exception(verify));
        // }

        // [Fact(DisplayName = "Remove: Should remove an existing entry")]
        // public void RemoveEntry()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";

        //     loader.Set(key, Task.FromResult("Bar"));

        //     // act
        //     loader.Remove(key);

        //     // assert
        //     Task<string> loadResult = loader.LoadAsync(key);

        //     if (loadResult is IAsyncResult asyncResult)
        //     {
        //         asyncResult.AsyncWaitHandle.WaitOne();
        //     }

        //     Assert.NotNull(loadResult.Exception);
        // }

        // #endregion

        // #region Set

        // [Fact(DisplayName = "Set: Should throw an argument null exception for key")]
        // public void SetKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     string key = null;
        //     var value = Task.FromResult("Foo");

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("key", verify);
        // }

        // [Fact(DisplayName = "Set: Should throw an argument null exception for value")]
        // public void SetValueNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";
        //     Task<string> value = null;

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("value", verify);
        // }

        // [Fact(DisplayName = "Set: Should not throw any exception")]
        // public void SetNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";
        //     var value = Task.FromResult("Bar");

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Null(Record.Exception(verify));
        // }

        // [Fact(DisplayName = "Set: Should result in a new cache entry")]
        // public async Task SetNewCacheEntry()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";
        //     var value = Task.FromResult("Bar");

        //     // act
        //     loader.Set(key, value);

        //     // assert
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     Assert.Equal(value.Result, loadResult);
        // }

        // [Fact(DisplayName = "Set: Should result in 'Bar'")]
        // public async Task SetTwice()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     var loader = new DataLoader<string, string>(options, fetch);
        //     var key = "Foo";
        //     var first = Task.FromResult("Bar");
        //     var second = Task.FromResult("Baz");

        //     // act
        //     loader.Set(key, first);
        //     loader.Set(key, second);

        //     // assert
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     Assert.Equal(first.Result, loadResult);
        // }

        // #endregion

        // #region IDataLoader.LoadAsync(object key)

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for key")]
        // public async Task IDataLoaderLoadSingleKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     string key = null;

        //     // act
        //     Func<Task<object>> verify = () => loader.LoadAsync(key);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("key", verify)
        //         .ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should not throw any exception")]
        // public async Task IDataLoaderLoadSingleNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";

        //     // act
        //     Func<Task<object>> verify = () => loader.LoadAsync(key);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify)
        //         .ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        // public async Task IDataLoaderLoadSingleResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";

        //     // act
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     // assert
        //     Assert.Equal(expectedResult.Value, loadResult);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one error")]
        // public async Task IDataLoaderLoadSingleErrorResult()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";

        //     // act
        //     Func<Task> verify = () => loader.LoadAsync(key);

        //     // assert
        //     await Assert.ThrowsAsync<InvalidOperationException>(verify)
        //         .ConfigureAwait(false);
        // }

        // #endregion

        // #region IDataLoader.LoadAsync(params string[] keys)

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
        // public async Task IDataLoaderLoadParamsKeysNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     string[] keys = null;

        //     // act
        //     Func<Task<IReadOnlyList<object>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("keys", verify)
        //         .ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
        // public async Task IDataLoaderLoadParamsZeroKeys()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new string[0];

        //     // act
        //     IReadOnlyList<object> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Empty(loadResult);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should not throw any exception")]
        // public async Task IDataLoaderLoadParamsNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new string[] { "Foo" };

        //     // act
        //     Func<Task<IReadOnlyList<object>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify)
        //         .ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        // public async Task IDataLoaderLoadParamsResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new string[] { "Foo" };

        //     // act
        //     IReadOnlyList<object> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Collection(loadResult,
        //         r => Assert.Equal(expectedResult.Value, r));
        // }

        // #endregion

        // #region IDataLoader.LoadAsync(IReadOnlyCollection<string> keys)

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for keys")]
        // public async Task IDataLoaderLoadCollectionKeysNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     List<string> keys = null;

        //     // act
        //     Func<Task<IReadOnlyList<object>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     await Assert.ThrowsAsync<ArgumentNullException>("keys", verify)
        //         .ConfigureAwait(false);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should allow empty list of keys")]
        // public async Task IDataLoaderLoadCollectionZeroKeys()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new List<string>();

        //     // act
        //     IReadOnlyList<object> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Empty(loadResult);
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should not throw any exception")]
        // public async Task IDataLoaderLoadCollectionNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>("Bar");
        //     var options = new DataLoaderOptions<string>()
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new List<string> { "Foo" };

        //     // act
        //     Func<Task<IReadOnlyList<object>>> verify = () =>
        //         loader.LoadAsync(keys);

        //     // assert
        //     Assert.Null(await Record.ExceptionAsync(verify)
        //         .ConfigureAwait(false));
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should return one result")]
        // public async Task IDataLoaderLoadCollectionResult()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>(expectedResult);
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var keys = new List<string> { "Foo" };

        //     // act
        //     IReadOnlyList<object> loadResult = await loader
        //         .LoadAsync(keys)
        //         .ConfigureAwait(false);

        //     // assert
        //     Assert.Collection(loadResult,
        //         v => Assert.Equal(expectedResult.Value, v));
        // }

        // [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw one exception for not existing value regarding key 'Qux'")]
        // public async Task IDataLoaderLoadAutoDispatching()
        // {
        //     // arrange
        //     Result<string> expectedResult = "Bar";
        //     var repository = new Dictionary<string, string>
        //     {
        //         { "Foo", "Bar" },
        //         { "Bar", "Baz" },
        //         { "Baz", "Foo" }
        //     };
        //     FetchDataDelegate<string, string> fetch =
        //         async (keys, cancellationToken) =>
        //         {
        //             var values = new List<Result<string>>();

        //             foreach (var key in keys)
        //             {
        //                 if (repository.ContainsKey(key))
        //                 {
        //                     values.Add(repository[key]);
        //                 }
        //                 else
        //                 {
        //                     var error = new Exception($"Value for key " +
        //                         $"\"{key}\" not found");

        //                     values.Add(error);
        //                 }
        //             }

        //             return await Task.FromResult(values).ConfigureAwait(false);
        //         };
        //     var options = new DataLoaderOptions<string>
        //     {
        //         AutoDispatching = true
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var requestKeys = new [] { "Foo", "Bar", "Baz", "Qux" };

        //     // act
        //     Func<Task> verify = () => loader.LoadAsync(requestKeys);

        //     // assert
        //     await Assert.ThrowsAsync<Exception>(verify).ConfigureAwait(false);
        // }

        // #endregion

        // #region IDataLoader.Remove

        // [Fact(DisplayName = "IDataLoader.Remove: Should throw an argument null exception for key")]
        // public void IDataLoaderRemoveKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     string key = null;

        //     loader.Set("Foo", Task.FromResult((object)"Bar"));

        //     // act
        //     Action verify = () => loader.Remove(key);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("key", verify);
        // }

        // [Fact(DisplayName = "IDataLoader.Remove: Should not throw any exception")]
        // public void IDataLoaderRemoveNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";

        //     // act
        //     Action verify = () => loader.Remove(key);

        //     // assert
        //     Assert.Null(Record.Exception(verify));
        // }

        // [Fact(DisplayName = "IDataLoader.Remove: Should remove an existing entry")]
        // public void IDataLoaderRemoveEntry()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>
        //     {
        //         Batch = false
        //     };
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";

        //     loader.Set(key, Task.FromResult((object)"Bar"));

        //     // act
        //     loader.Remove(key);

        //     // assert
        //     Task<object> loadResult = loader.LoadAsync(key);

        //     if (loadResult is IAsyncResult asyncResult)
        //     {
        //         asyncResult.AsyncWaitHandle.WaitOne();
        //     }

        //     Assert.NotNull(loadResult.Exception);
        // }

        // #endregion

        // #region IDataLoader.Set

        // [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for key")]
        // public void IDataLoaderSetKeyNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     string key = null;
        //     var value = Task.FromResult((object)"Foo");

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("key", verify);
        // }

        // [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for value")]
        // public void IDataLoaderSetValueNull()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";
        //     Task<object> value = null;

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Throws<ArgumentNullException>("value", verify);
        // }

        // [Fact(DisplayName = "IDataLoader.Set: Should not throw any exception")]
        // public void IDataLoaderSetNoException()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";
        //     var value = Task.FromResult((object)"Bar");

        //     // act
        //     Action verify = () => loader.Set(key, value);

        //     // assert
        //     Assert.Null(Record.Exception(verify));
        // }

        // [Fact(DisplayName = "IDataLoader.Set: Should result in a new cache entry")]
        // public async Task IDataLoaderSetNewCacheEntry()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";
        //     var value = Task.FromResult((object)"Bar");

        //     // act
        //     loader.Set(key, value);

        //     // assert
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     Assert.Equal(value.Result, loadResult);
        // }

        // [Fact(DisplayName = "IDataLoader.Set: Should result in 'Bar'")]
        // public async Task IDataLoaderSetTwice()
        // {
        //     // arrange
        //     FetchDataDelegate<string, string> fetch = TestHelpers
        //         .CreateFetch<string, string>();
        //     var options = new DataLoaderOptions<string>();
        //     IDataLoader loader = new DataLoader<string, string>(options,
        //         fetch);
        //     var key = "Foo";
        //     var first = Task.FromResult((object)"Bar");
        //     var second = Task.FromResult((object)"Baz");

        //     // act
        //     loader.Set(key, first);
        //     loader.Set(key, second);

        //     // assert
        //     var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

        //     Assert.Equal(first.Result, loadResult);
        // }

        // #endregion
    }
}
