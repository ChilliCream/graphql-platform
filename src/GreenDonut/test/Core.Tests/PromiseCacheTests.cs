using Xunit;

namespace GreenDonut;

public class PromiseCacheTests
{
    [Fact(DisplayName = "Constructor: Should not throw any exception")]
    public void ConstructorNoException()
    {
        // arrange
        var cacheSize = 1;

        // act
        void Verify() => new PromiseCache(cacheSize);

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
        var cache = new PromiseCache(cacheSize);

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
        var cache = new PromiseCache(cacheSize);

        foreach (var value in values)
        {
            cache.TryAdd(new PromiseCacheKey("a", $"Key:{value}"), new Promise<string>(value));
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
        var cache = new PromiseCache(cacheSize);

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
        var cache = new PromiseCache(cacheSize);

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
        var cache = new PromiseCache(cacheSize);

        cache.TryAdd(new PromiseCacheKey("a", "Foo"), new Promise<string>("Bar"));
        cache.TryAdd(new PromiseCacheKey("a", "Bar"), new Promise<string>("Baz"));

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
        var cache = new PromiseCache(cacheSize);
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
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");
        var value = Task.FromResult("Bar");

        cache.TryAdd(key, new Promise<string>(value));

        // act
        cache.TryRemove(key);

        // assert
        var retrieved = cache.GetOrAddTask(key, _ => new Promise<string>("Baz"));
        Assert.NotSame(value, retrieved);
    }

    [Fact(DisplayName = "TryAdd: Should throw an argument null exception for value")]
    public void TryAddValueNull()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");

        // act
        void Verify() => cache.TryAdd(key, default(Promise<string>));

        // assert
        Assert.Throws<ArgumentNullException>("promise", Verify);
    }

    [Fact(DisplayName = "TryAdd: Should result in a new cache entry")]
    public void TryAddNewCacheEntry()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");
        var expected = new Promise<string>("Bar");

        // act
        var added = cache.TryAdd(key, expected);

        // assert
        var resolved = cache.GetOrAddTask(key, _ => new Promise<string>("Baz"));

        Assert.True(added);
        Assert.Equal(expected.Task, resolved);
    }

    [Fact(DisplayName = "TryAdd: Should result in a new cache entry and use the factory")]
    public void TryAddNewCacheEntryWithFactory()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");
        var expected = new Promise<string>(Task.FromResult("Bar"));

        // act
        var added = cache.TryAdd(key, () => expected);

        // assert
        var resolved = cache.GetOrAddTask(key, _ => new Promise<string>(Task.FromResult("Baz")));

        Assert.True(added);
        Assert.Same(expected.Task, resolved);
    }

    [Fact(DisplayName = "TryAdd: Should result in 'Bar'")]
    public void TryAddTwice()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");
        var expected = Task.FromResult("Bar");
        var another = Task.FromResult("Baz");

        // act
        var addedFirst = cache.TryAdd(key, new Promise<string>(expected));
        var addedSecond = cache.TryAdd(key, new Promise<string>(another));

        // assert
        var resolved = cache.GetOrAddTask(key, _ => new Promise<string>(Task.FromResult("Quox")));

        Assert.True(addedFirst);
        Assert.False(addedSecond);
        Assert.Same(expected, resolved);
    }

    [Fact(DisplayName = "GetOrAddTask: Should return new item if nothing is cached")]
    public async Task GetOrAddTaskWhenNothingIsCached()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", "Foo");

        // act
        var resolved = cache.GetOrAddTask(key, _ => new Promise<string>(Task.FromResult("Quox")));

        // assert
        Assert.Equal("Quox", await resolved);
    }

    [Fact(DisplayName = "TryGetValue (String): Should return one result")]
    public async Task GetOrAddTaskWhenNothingIsCached_IntegerKey()
    {
        // arrange
        var cacheSize = 10;
        var cache = new PromiseCache(cacheSize);
        var key = new PromiseCacheKey("a", 1);

        // act
        var resolved = cache.GetOrAddTask(key, _ => new Promise<string>(Task.FromResult("Quox")));

        // assert
        Assert.Equal("Quox", await resolved);
    }
}
