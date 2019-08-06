using System;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut
{
    public class TaskCacheTests
    {
        #region Constructor

        [Fact(DisplayName = "Constructor: Should not throw any exception")]
        public void ConstructorNoException()
        {
            // arrange
            var cacheSize = 1;
            TimeSpan slidingExpiration = TimeSpan.Zero;

            // act
            Action verify = () => new TaskCache<string>(cacheSize,
                slidingExpiration);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        #endregion

        #region Size

        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 1000)]
        [Theory(DisplayName = "Size: Should return the expected cache size")]
        public void Size(int cacheSize, int expectedCacheSize)
        {
            // arrange
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            // act
            var result = cache.Size;

            // assert
            Assert.Equal(expectedCacheSize, result);
        }

        #endregion

        #region SlidingExpirartion

        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 1000)]
        [Theory(DisplayName = "SlidingExpirartion: Should return the expected sliding expiration")]
        public void SlidingExpirartion(
            int expirationInMilliseconds,
            int expectedExpirationInMilliseconds)
        {
            // arrange
            var cacheSize = 10;
            var slidingExpiration = TimeSpan
                .FromMilliseconds(expirationInMilliseconds);
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            // act
            TimeSpan result = cache.SlidingExpirartion;

            // assert
            Assert.Equal(expectedExpirationInMilliseconds,
                result.TotalMilliseconds);
        }

        #endregion

        #region Usage

        [InlineData(new string[] { "Foo" }, 1)]
        [InlineData(new string[] { "Foo", "Bar" }, 2)]
        [InlineData(new string[] { "Foo", "Bar", "Baz" }, 3)]
        [InlineData(new string[] { "Foo", "Bar", "Baz", "Qux", "Quux", "Corge",
            "Grault", "Graply", "Waldo", "Fred", "Plugh", "xyzzy" }, 10)]
        [Theory(DisplayName = "Usage: Should return the expected cache usage")]
        public void Usage(string[] values, int expectedUsage)
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            foreach (var value in values)
            {
                cache.TryAdd($"Key:{value}", Task.FromResult(value));
            }

            // act
            var result = cache.Usage;

            // assert
            Assert.Equal(expectedUsage, result);
        }

        #endregion

        #region Clear

        [Fact(DisplayName = "Clear: Should not throw any exception")]
        public void ClearNoException()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            // act
            Action verify = () => cache.Clear();

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Clear: Should clear empty cache")]
        public void ClearEmptyCache()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            // act
            cache.Clear();

            // assert
            Assert.Equal(0, cache.Usage);
        }

        [Fact(DisplayName = "Clear: Should remove all entries from the cache")]
        public void ClearAllEntries()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);

            cache.TryAdd("Foo", Task.FromResult("Bar"));
            cache.TryAdd("Bar", Task.FromResult("Baz"));

            // act
            cache.Clear();

            // assert
            Assert.Equal(0, cache.Usage);
        }

        #endregion

        #region Remove

        [Fact(DisplayName = "Remove: Should not throw any exception")]
        public void RemoveNoException()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";

            // act
            Action verify = () => cache.Remove(key);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Remove: Should remove an existing entry")]
        public void RemoveEntry()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";

            cache.TryAdd(key, Task.FromResult("Bar"));

            // act
            cache.Remove(key);

            // assert
            var exists = cache.TryGetValue(key, out Task<string> actual);

            Assert.False(exists);
            Assert.Null(actual);
        }

        #endregion

        #region TryAdd

        [Fact(DisplayName = "TryAdd: Should throw an argument null exception for value")]
        public void TryAddValueNull()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";
            Task<string> value = null;

            // act
            Action verify = () => cache.TryAdd(key, value);

            // assert
            Assert.Throws<ArgumentNullException>("value", verify);
        }

        [Fact(DisplayName = "TryAdd: Should not throw any exception")]
        public void TryAddNoException()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";
            var value = Task.FromResult("Bar");

            // act
            Action verify = () => cache.TryAdd(key, value);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "TryAdd: Should result in a new cache entry")]
        public async Task TryAddNewCacheEntry()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";
            var expected = Task.FromResult("Bar");

            // act
            var added = cache.TryAdd(key, expected);

            // assert
            var exists = cache.TryGetValue(key, out Task<string> actual);

            Assert.True(added);
            Assert.True(exists);
            Assert.Equal(await expected.ConfigureAwait(false),
                await actual.ConfigureAwait(false));
        }

        [Fact(DisplayName = "TryAdd: Should result in 'Bar'")]
        public async Task TryAddTwice()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";
            var expected = Task.FromResult("Bar");
            var another = Task.FromResult("Baz");

            // act
            var addedFirst = cache.TryAdd(key, expected);
            var addedSecond = cache.TryAdd(key, another);

            // assert
            var exists = cache.TryGetValue(key, out Task<string> actual);

            Assert.True(addedFirst);
            Assert.False(addedSecond);
            Assert.True(exists);
            Assert.Equal(await expected.ConfigureAwait(false),
                await actual.ConfigureAwait(false));
        }

        #endregion

        #region TryGetValue

        [Fact(DisplayName = "TryGetValue: Should return false")]
        public void TryGetValueNullResult()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";

            // act
            var result = cache.TryGetValue(key, out Task<string> value);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "TryGetValue (String): Should return one result")]
        public async Task TryGetValueResultByString()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";
            var expected = Task.FromResult("Bar");

            cache.TryAdd(key, expected);

            // act
            var result = cache.TryGetValue(key, out Task<string> actual);

            // assert
            Assert.True(result);
            Assert.Equal(await expected.ConfigureAwait(false),
                await actual.ConfigureAwait(false));
        }

        [Fact(DisplayName = "TryGetValue (Integer): Should return one result")]
        public async Task TryGetValueResultByInteger()
        {
            // arrange
            var cacheSize = 10;
            TimeSpan slidingExpiration = TimeSpan.Zero;
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = 1;
            var expected = Task.FromResult("Bar");

            cache.TryAdd(key, expected);

            // act
            var result = cache.TryGetValue(key, out Task<string> actual);

            // assert
            Assert.True(result);
            Assert.Equal(await expected.ConfigureAwait(false),
                await actual.ConfigureAwait(false));
        }

        #endregion

        #region Expiration

        [Fact(DisplayName = "VerifyExpirationFalse: Should return false if expired")]
        public async Task VerifyExpirationFalse()
        {
            // arrange
            var cacheSize = 10;
            var slidingExpiration = TimeSpan.FromMilliseconds(100);
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";

            cache.TryAdd(key, Task.FromResult("Bar"));
            await Task.Delay(300).ConfigureAwait(false);

            // act
            var exists = cache.TryGetValue(key, out Task<string> actual);

            // assert
            Assert.False(exists);
        }

        [Fact(DisplayName = "VerifyExpirationTrue: Should return true if not expired")]
        public void VerifyExpirationTrue()
        {
            // arrange
            var cacheSize = 10;
            var slidingExpiration = TimeSpan.FromMilliseconds(500);
            var cache = new TaskCache<string>(cacheSize,
                slidingExpiration);
            var key = "Foo";

            cache.TryAdd(key, Task.FromResult("Bar"));

            // act
            var exists = cache.TryGetValue(key, out Task<string> actual);

            // assert
            Assert.True(exists);
        }

        #endregion
    }
}
