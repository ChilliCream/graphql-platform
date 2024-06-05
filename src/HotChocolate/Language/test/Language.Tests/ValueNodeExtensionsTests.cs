using Xunit;

namespace HotChocolate.Language;

public static class ValueNodeExtensionsTests
{
    [Fact]
    public static void IsNull_Null_True()
    {
        // arrange
        var value = default(IValueNode);

        // act
        var result = value.IsNull();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsNull_NullValueNode_True()
    {
        // arrange
        IValueNode value = NullValueNode.Default;

        // act
        var result = value.IsNull();

        // assert
        Assert.True(result);
    }

    [Fact]
    public static void IsNull_StringValueNode_False()
    {
        // arrange
        IValueNode value = new StringValueNode("foo");

        // act
        var result = value.IsNull();

        // assert
        Assert.False(result);
    }
}
