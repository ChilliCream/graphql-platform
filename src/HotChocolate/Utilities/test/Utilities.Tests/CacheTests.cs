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
        var value = cache.GetOrCreate("10", _ => "10");

        // assert
        Assert.Equal("10", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("10", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("1", key),
            key => Assert.Equal("0", key));
    }

    [Fact]
    public void Add_More_Items_To_The_Cache_Than_We_Have_Space()
    {
        // arrange
        var cache = new Cache<string>(10);

        for (var i = 0; i < 10; i++)
        {
            var item = i.ToString();
            cache.GetOrCreate(item, _ => item);
        }

        // assert
        var value = cache.GetOrCreate("11", _ => "11");

        // assert
        Assert.Equal("11", value);
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(10, cache.Count);

        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("10", key),
            key => Assert.Equal("9", key),
            key => Assert.Equal("8", key),
            key => Assert.Equal("7", key),
            key => Assert.Equal("6", key),
            key => Assert.Equal("5", key),
            key => Assert.Equal("4", key),
            key => Assert.Equal("3", key),
            key => Assert.Equal("2", key),
            key => Assert.Equal("1", key));
    }

    [Fact]
    public void Avoid_Item_Reorder_If_Cache_Is_Not_Full()
    {
        // arrange
        var cache = new Cache<string>(10);
        cache.GetOrCreate("a", _ => "a");
        cache.GetOrCreate("b", _ => "b");
        cache.GetOrCreate("c", _ => "c");
        cache.GetOrCreate("d", _ => "d");
        cache.GetOrCreate("e", _ => "e");
        cache.GetOrCreate("f", _ => "f");

        // act
        Assert.True(cache.TryGet("c", out _));

        // assert
        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("f", key),
            key => Assert.Equal("e", key),
            key => Assert.Equal("d", key),
            key => Assert.Equal("c", key),
            key => Assert.Equal("b", key),
            key => Assert.Equal("a", key));
    }

    [Fact]
    public void Reorder_Items_When_Cache_Is_Filling_Up_With_TryGet()
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

        // act
        Assert.True(cache.TryGet("c", out _));

        // assert
        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("c", key),
            key => Assert.Equal("g", key),
            key => Assert.Equal("f", key),
            key => Assert.Equal("e", key),
            key => Assert.Equal("d", key),
            key => Assert.Equal("b", key),
            key => Assert.Equal("a", key));
    }

    [Fact]
    public void Reorder_Items_When_Cache_Is_Filling_Up_With_GetOrCreate()
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

        // act
        cache.GetOrCreate("c", _ => "c");

        // assert
        Assert.Collection(
            cache.GetKeys(),
            key => Assert.Equal("c", key),
            key => Assert.Equal("g", key),
            key => Assert.Equal("f", key),
            key => Assert.Equal("e", key),
            key => Assert.Equal("d", key),
            key => Assert.Equal("b", key),
            key => Assert.Equal("a", key));
    }
}
