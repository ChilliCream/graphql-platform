using Xunit;

namespace HotChocolate.Utilities;

public class CacheEntryEventArgsTests
{
    [Fact]
    public void ValueIsNull()
    {
        // act
        var eventArgs = new CacheEntryEventArgs<string?>("key", null);

        // assert
        Assert.Equal("key", eventArgs.Key);
        Assert.Null(eventArgs.Value);
    }

    [Fact]
    public void ValueAndKeyAreSet()
    {
        // act
        var eventArgs = new CacheEntryEventArgs<string>("key", "value");

        // assert
        Assert.Equal("key", eventArgs.Key);
        Assert.Equal("value", eventArgs.Value);
    }

    [Fact]
    public void KeyIsNull()
    {
        // act
        Action action = () => new CacheEntryEventArgs<string>(null!, "value");

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
