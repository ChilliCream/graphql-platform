using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut
{
    public class DataLoaderExtensionsTests
    {
        [Fact(DisplayName = "Set: Should throw an argument null exception for dataLoader")]
        public void SetDataLoaderNull()
        {
            // arrange
            IDataLoader<string, string> loader = null;
            var key = "Foo";
            var value = "Bar";

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "Set: Should throw an argument null exception for key")]
        public void SetKeyNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            string key = null;
            var value = "Bar";

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "Set: Should not throw any exception")]
        public void SetNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";
            string value = null;

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Set: Should result in a new cache entry")]
        public async Task SetNewCacheEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";
            var value = "Bar";

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
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";
            var first = "Bar";
            var second = "Baz";

            // act
            loader.Set(key, first);
            loader.Set(key, second);

            // assert
            var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

            Assert.Equal(first, loadResult);
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for dataLoader")]
        public void LoadSingleDataLoaderNull()
        {
            // arrange
            IDataLoader<string, string> loader = null;
            var key = "Foo";

            // act
            Action verify = () => loader.LoadAsync(key);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        public void LoadSingleNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);
            var key = "Foo";

            // act
            Action verify = () => loader.LoadAsync(key);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for dataLoader")]
        public void LoadParamsDataLoaderNull()
        {
            // arrange
            IDataLoader<string, string> loader = null;

            // act
            Action verify = () => loader.LoadAsync(new string[0]);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        public void LoadParamsNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.LoadAsync(new string[0]);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "LoadAsync: Should throw an argument null exception for dataLoader")]
        public void LoadCollectionDataLoaderNull()
        {
            // arrange
            IDataLoader<string, string> loader = null;

            // act
            Action verify = () => loader.LoadAsync(new List<string>());

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        public void LoadCollectionNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            var loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.LoadAsync(new List<string>());

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for dataLoader")]
        public void IDataLoaderLoadSingleDataLoaderNull()
        {
            // arrange
            IDataLoader loader = null;
            object key = "Foo";

            // act
            Action verify = () => loader.LoadAsync(key);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should not throw any exception")]
        public void IDataLoaderLoadSingleNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";

            // act
            Action verify = () => loader.LoadAsync(key);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for dataLoader")]
        public void IDataLoaderLoadParamsDataLoaderNull()
        {
            // arrange
            IDataLoader loader = null;

            // act
            Action verify = () => loader.LoadAsync(new object[0]);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "LoadAsync: Should not throw any exception")]
        public void IDataLoaderLoadParamsNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.LoadAsync(new object[0]);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should throw an argument null exception for dataLoader")]
        public void IDataLoaderLoadCollectionDataLoaderNull()
        {
            // arrange
            IDataLoader loader = null;

            // act
            Action verify = () => loader.LoadAsync(new List<object>());

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "IDataLoader.LoadAsync: Should not throw any exception")]
        public void IDataLoaderLoadCollectionNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers
                .CreateFetch<string, string>("Bar");
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);

            // act
            Action verify = () => loader.LoadAsync(new List<object>());

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for dataLoader")]
        public void IDataLoaderSetDataLoaderNull()
        {
            // arrange
            IDataLoader loader = null;
            object key = "Foo";
            object value = "Bar";

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("dataLoader", verify);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should throw an argument null exception for key")]
        public void IDataLoaderSetKeyNull()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = null;
            object value = "Bar";

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("key", verify);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should not throw any exception")]
        public void IDataLoaderSetNoException()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";
            object value = null;

            // act
            Action verify = () => loader.Set(key, value);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "IDataLoader.Set: Should result in a new cache entry")]
        public async Task IDataLoaderSetNewCacheEntry()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";
            object value = "Bar";

            // act
            loader.Set(key, value);

            // assert
            var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

            Assert.Equal(value, loadResult);
        }

        [Fact(DisplayName = "IDataLoader.Set: Should result in 'Bar'")]
        public async Task IDataLoaderSetTwice()
        {
            // arrange
            FetchDataDelegate<string, string> fetch = TestHelpers.CreateFetch<string, string>();
            var batchScheduler = new ManualBatchScheduler();
            IDataLoader loader = new DataLoader<string, string>(batchScheduler, fetch);
            object key = "Foo";
            object first = "Bar";
            object second = "Baz";

            // act
            loader.Set(key, first);
            loader.Set(key, second);

            // assert
            var loadResult = await loader.LoadAsync(key).ConfigureAwait(false);

            Assert.Equal(first, loadResult);
        }
    }
}
