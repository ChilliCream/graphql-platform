using System;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut;

public class TaskCacheTests
{
    [Fact(DisplayName = "Constructor: Should not throw any exception")]
    public void ConstructorNoException()
    {
        // arrange
        var cacheSize = 1;

        // act
        void Verify() => new TaskCache(cacheSize);

        // assert
        Assert.Null(Record.Exception(Verify));
    }

    [InlineData(0, 10)]
    [InlineData(1, 10)]
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

    [InlineData(new[] { "Foo", }, 1)]
    [InlineData(new[] { "Foo", "Bar", }, 2)]
    [InlineData(new[] { "Foo", "Bar", "Baz", }, 3)]
    [InlineData(new[] { "Foo", "Bar", "Baz", "Qux", "Quux", "Corge",
        "Grault", "Graply", "Waldo", "Fred", "Plugh", "xyzzy", }, 10)]
    [Theory(DisplayName = "Usage: Should return the expected cache usage")]
    public void Usage(string[] values, int expectedUsage)
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);

        foreach (var value in values)
        {
            cache.TryAdd(new TaskCacheKey("a", $"Key:{value}"), Task.FromResult(value));
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
        void Verify() => cache.Clear();

        // assert
        Assert.Null(Record.Exception(Verify));
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

        cache.TryAdd(new TaskCacheKey("a", "Foo"), Task.FromResult("Bar"));
        cache.TryAdd(new TaskCacheKey("a", "Bar"), Task.FromResult("Baz"));

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
        bool Verify() => cache.TryRemove(new("a", key));

        // assert
        Assert.False(Verify());
    }

    [Fact(DisplayName = "Remove: Should remove an existing entry")]
    public void RemoveEntry()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");
        var value = Task.FromResult("Bar");

        cache.TryAdd(key, value);

        // act
        cache.TryRemove(key);

        // assert
        var retrieved = cache.GetOrAddTask(key, () => Task.FromResult("Baz"));
        Assert.NotSame(value, retrieved);
    }

    [Fact(DisplayName = "TryAdd: Should throw an argument null exception for value")]
    public void TryAddValueNull()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");

        // act
        void Verify() => cache.TryAdd(key, default(Task<string>)!);

        // assert
        Assert.Throws<ArgumentNullException>("value", Verify);
    }

    [Fact(DisplayName = "TryAdd: Should result in a new cache entry")]
    public void TryAddNewCacheEntry()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");
        var expected = Task.FromResult("Bar");

        // act
        var added = cache.TryAdd(key, expected);

        // assert
        var resolved = cache.GetOrAddTask(key, () => Task.FromResult("Baz"));

        Assert.True(added);
        Assert.Same(expected, resolved);
    }

    [Fact(DisplayName = "TryAdd: Should result in a new cache entry and use the factory")]
    public void TryAddNewCacheEntryWithFactory()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");
        var expected = Task.FromResult("Bar");

        // act
        var added = cache.TryAdd(key, () => expected);

        // assert
        var resolved = cache.GetOrAddTask(key, () => Task.FromResult("Baz"));

        Assert.True(added);
        Assert.Same(expected, resolved);
    }

    [Fact(DisplayName = "TryAdd: Should result in 'Bar'")]
    public void TryAddTwice()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");
        var expected = Task.FromResult("Bar");
        var another = Task.FromResult("Baz");

        // act
        var addedFirst = cache.TryAdd(key, expected);
        var addedSecond = cache.TryAdd(key, another);

        // assert
        var resolved = cache.GetOrAddTask(key, () => Task.FromResult("Quox"));

        Assert.True(addedFirst);
        Assert.False(addedSecond);
        Assert.Same(expected, resolved);
    }

    [Fact(DisplayName = "GetOrAddTask: Should return new item if nothing is cached")]
    public void GetOrAddTaskWhenNothingIsCached()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", "Foo");

        // act
        var resolved = cache.GetOrAddTask(key, () => Task.FromResult("Quox"));

        // assert
        Assert.Equal("Quox", resolved.Result);
    }

    [Fact(DisplayName = "TryGetValue (String): Should return one result")]
    public void GetOrAddTaskWhenNothingIsCached_IntegerKey()
    {
        // arrange
        var cacheSize = 10;
        var cache = new TaskCache(cacheSize);
        var key = new TaskCacheKey("a", 1);

        // act
        var resolved = cache.GetOrAddTask(key, () => Task.FromResult("Quox"));

        // assert
        Assert.Equal("Quox", resolved.Result);
    }
}