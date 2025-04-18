using Xunit;

namespace HotChocolate.Utilities;

public class CacheTests
{
    [Fact]
    public void Fill_Cache_Up()
    {
        // arrange
        var cache = new Cache<string>(10);

        for (var i = 0; i < 9; i++)
        {
            var item = i.ToString();
            cache.GetOrCreate(item, _ => item);
        }

        // assert
        var value = cache.GetOrCreate("9", _ => "9");

        // assert
        Assert.Equal("9", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("0", key),
            key => Assert.Equal("1", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("9", key));
    }

    [Fact]
    public void Add_More_Items_To_The_Cache_Than_We_Have_Space()
    {
        // arrange
        var cache = new Cache<string>(10);

        // fill up all slots.
        for (var i = 0; i < 10; i++)
        {
            var item = i.ToString();
            cache.GetOrCreate(item, _ => item);
        }

        // assert
        // Adds and 11th item that will cause the eviction of an item.
        var value = cache.GetOrCreate("10", _ => "10");

        // assert
        Assert.Equal("10", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            // item 0 was evicted from slot 0
            key => Assert.Equal("10", key),
            key => Assert.Equal("1", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("9", key));
    }

    [Fact]
    public void Evict_Items_That_Were_Not_Recently_Access_When_Cache_Is_Full()
    {
        // arrange
        var cache = new Cache<string>(10);
        cache.GetOrCreate("a", _ => "a");
        cache.GetOrCreate("b", _ => "b");
        cache.GetOrCreate("c", _ => "c");
        cache.GetOrCreate("d", _ => "d");
        cache.GetOrCreate("e", _ => "e");
        cache.GetOrCreate("f", _ => "f");
        cache.GetOrCreate("g", _ => "g");
        cache.GetOrCreate("h", _ => "h");
        cache.GetOrCreate("i", _ => "i");
        cache.GetOrCreate("j", _ => "j");
        cache.GetOrCreate("x", _ => "x");

        // act
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.True(cache.TryGet("d", out _));
        cache.GetOrCreate("y", _ => "y");
        Assert.True(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.True(cache.TryGet("d", out _));
        cache.GetOrCreate("z", _ => "z");


        // assert
        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("x", key),
            key => Assert.Equal("b", key),
            key => Assert.Equal("c", key),
            key => Assert.Equal("d", key),
            key => Assert.Equal("y", key),
            key => Assert.Equal("z", key),
            key => Assert.Equal("g", key),
            key => Assert.Equal("h", key),
            key => Assert.Equal("i", key),
            key => Assert.Equal("j", key));
    }
}
