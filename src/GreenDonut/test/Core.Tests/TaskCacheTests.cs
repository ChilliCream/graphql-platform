using System;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut
{
    public class TaskCacheTests
    {
        [Fact(DisplayName = "Constructor: Should not throw any exception")]
        public void ConstructorNoException()
        {
            // arrange
            var cacheSize = 1;

            // act
            Action verify = () => new TaskCache(cacheSize);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 1000)]
        [Theory(DisplayName = "Size: Should return the expected cache size")]
        public void Size(int cacheSize, int expectedCacheSize)
        {
            // arrange
            var cache = new TaskCache(cacheSize);

            // act
            var result = cache.Size;

            // assert
            Assert.Equal(expectedCacheSize, result);
        }

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
            var cache = new TaskCache(cacheSize);

            foreach (var value in values)
            {
                cache.TryAdd($"Key:{value}", value);
            }

            // act
            var result = cache.Usage;

            // assert
            Assert.Equal(expectedUsage, result);
        }

        [Fact(DisplayName = "Clear: Should not throw any exception")]
        public void ClearNoException()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);

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
            var cache = new TaskCache(cacheSize);

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
            var cache = new TaskCache(cacheSize);

            cache.TryAdd("Foo", Task.FromResult("Bar"));
            cache.TryAdd("Bar", Task.FromResult("Baz"));

            // act
            cache.Clear();

            // assert
            Assert.Equal(0, cache.Usage);
        }

        [Fact(DisplayName = "Remove: Should not throw any exception")]
        public void RemoveNoException()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
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
            var cache = new TaskCache(cacheSize);
            var key = "Foo";

            cache.TryAdd(key, "Bar");

            // act
            cache.Remove(key);

            // assert
            var exists = cache.TryGetValue(key, out object actual);

            Assert.False(exists);
            Assert.Null(actual);
        }

        [Fact(DisplayName = "TryAdd: Should throw an argument null exception for value")]
        public void TryAddValueNull()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = "Foo";
            string value = null;

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
            var cache = new TaskCache(cacheSize);
            var key = "Foo";
            var value = "Bar";

            // act
            Action verify = () => cache.TryAdd(key, value);

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "TryAdd: Should result in a new cache entry")]
        public void TryAddNewCacheEntry()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = "Foo";
            var expected = "Bar";

            // act
            var added = cache.TryAdd(key, expected);

            // assert
            var exists = cache.TryGetValue(key, out object actual);

            Assert.True(added);
            Assert.True(exists);
            Assert.Equal(expected, (string)actual);
        }

        [Fact(DisplayName = "TryAdd: Should result in 'Bar'")]
        public void TryAddTwice()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = "Foo";
            var expected = "Bar";
            var another = "Baz";

            // act
            var addedFirst = cache.TryAdd(key, expected);
            var addedSecond = cache.TryAdd(key, another);

            // assert
            var exists = cache.TryGetValue(key, out object actual);

            Assert.True(addedFirst);
            Assert.False(addedSecond);
            Assert.True(exists);
            Assert.Equal(expected, (string)actual);
        }

        [Fact(DisplayName = "TryGetValue: Should return false")]
        public void TryGetValueNullResult()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = "Foo";

            // act
            var result = cache.TryGetValue(key, out object value);

            // assert
            Assert.False(result);
        }

        [Fact(DisplayName = "TryGetValue (String): Should return one result")]
        public void TryGetValueResultByString()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = "Foo";
            var expected = "Bar";

            cache.TryAdd(key, expected);

            // act
            var result = cache.TryGetValue(key, out object actual);

            // assert
            Assert.True(result);
            Assert.Equal(expected, (string)actual);
        }

        [Fact(DisplayName = "TryGetValue (Integer): Should return one result")]
        public void TryGetValueResultByInteger()
        {
            // arrange
            var cacheSize = 10;
            var cache = new TaskCache(cacheSize);
            var key = 1;
            var expected = "Bar";

            cache.TryAdd(key, expected);

            // act
            var result = cache.TryGetValue(key, out object actual);

            // assert
            Assert.True(result);
            Assert.Equal(expected, (string)actual);
        }
    }
}
