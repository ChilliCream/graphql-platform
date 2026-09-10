namespace HotChocolate.Caching.Memory;

public class CacheOfTKeyTests
{
    [Fact]
    public void GetOrCreate_Should_Fill_Cache_Up_When_Below_Capacity()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);

        for (var i = 0; i < 9; i++)
        {
            var item = new CacheKey(i);
            cache.GetOrCreate(item, key => key.Value.ToString());
        }

        // act
        var value = cache.GetOrCreate(new CacheKey(9), key => key.Value.ToString());

        // assert
        Assert.Equal("9", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal(new CacheKey(0), key),
            key => Assert.Equal(new CacheKey(1), key),
            key => Assert.Equal(new CacheKey(2), key),
            key => Assert.Equal(new CacheKey(3), key),
            key => Assert.Equal(new CacheKey(4), key),
            key => Assert.Equal(new CacheKey(5), key),
            key => Assert.Equal(new CacheKey(6), key),
            key => Assert.Equal(new CacheKey(7), key),
            key => Assert.Equal(new CacheKey(8), key),
            key => Assert.Equal(new CacheKey(9), key));
    }

    [Fact]
    public void GetOrCreate_Should_Evict_Oldest_When_Over_Capacity()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);

        for (var i = 0; i < 10; i++)
        {
            var item = new CacheKey(i);
            cache.GetOrCreate(item, key => key.Value.ToString());
        }

        // act
        var value = cache.GetOrCreate(new CacheKey(10), key => key.Value.ToString());

        // assert
        Assert.Equal("10", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            // item 0 was evicted from slot 0
            key => Assert.Equal(new CacheKey(10), key),
            key => Assert.Equal(new CacheKey(1), key),
            key => Assert.Equal(new CacheKey(2), key),
            key => Assert.Equal(new CacheKey(3), key),
            key => Assert.Equal(new CacheKey(4), key),
            key => Assert.Equal(new CacheKey(5), key),
            key => Assert.Equal(new CacheKey(6), key),
            key => Assert.Equal(new CacheKey(7), key),
            key => Assert.Equal(new CacheKey(8), key),
            key => Assert.Equal(new CacheKey(9), key));
    }

    [Fact]
    public void GetOrCreate_Should_Return_Existing_Value_When_Key_Present()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);
        var key = new CacheKey(1);
        var factoryInvocations = 0;

        // act
        var first = cache.GetOrCreate(
            key,
            _ =>
            {
                factoryInvocations++;
                return "first";
            });
        var second = cache.GetOrCreate(
            key,
            _ =>
            {
                factoryInvocations++;
                return "second";
            });

        // assert
        Assert.Equal("first", first);
        Assert.Equal("first", second);
        Assert.Equal(1, factoryInvocations);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void TryGet_Should_Return_False_When_Missing()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);

        // act
        var found = cache.TryGet(new CacheKey(1), out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_Should_Return_True_When_Present()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);
        cache.TryAdd(new CacheKey(1), "one");

        // act
        var found = cache.TryGet(new CacheKey(1), out var value);

        // assert
        Assert.True(found);
        Assert.Equal("one", value);
    }

    [Fact]
    public void GetOrCreate_Should_Throw_When_Create_Is_Null()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => cache.GetOrCreate<int>(new CacheKey(1), null!, 1));
    }

    [Fact]
    public void TryGet_Should_Keep_Distinct_Keys_Separate_When_Hashes_Collide()
    {
        // arrange
        var cache = new Cache<CacheKey, string>(10);
        var firstKey = new CacheKey(1);
        var secondKey = new CacheKey(2);
        cache.GetOrCreate(firstKey, _ => "first");
        cache.GetOrCreate(secondKey, _ => "second");

        // act
        var firstFound = cache.TryGet(firstKey, out var first);
        var secondFound = cache.TryGet(secondKey, out var second);

        // assert
        Assert.True(firstFound);
        Assert.True(secondFound);
        Assert.Equal("first", first);
        Assert.Equal("second", second);
    }

    private readonly record struct CacheKey(int Value)
    {
        public override int GetHashCode() => 1;
    }
}
