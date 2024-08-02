using System.Text;
using Xunit;
using static HotChocolate.Language.SyntaxComparison;

namespace HotChocolate.Language.SyntaxTree;

public class StringValueNodeTests
{
    [Fact]
    public void Create_StringValueNode_1()
    {
        // arrange
        // act
        var value = new StringValueNode("abc");

        // assert
        Assert.Equal("abc", value.Value);
        Assert.False(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Null(value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void Create_StringValueNode_2_Location_Is_Null()
    {
        // arrange
        // act
        var value = new StringValueNode(null, "abc", true);

        // assert
        Assert.Equal("abc", value.Value);
        Assert.True(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Null(value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void Create_StringValueNode_2_With_Location()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);

        // act
        var value = new StringValueNode(location, "abc", true);

        // assert
        Assert.Equal("abc", value.Value);
        Assert.True(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Same(location, value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void Create_StringValueNode_3_Location_Is_Null()
    {
        // arrange
        var stringValue = Encoding.UTF8.GetBytes("abc");

        // act
        var value = new StringValueNode(null, stringValue, true);

        // assert
        Assert.Equal("abc", value.Value);
        Assert.True(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Null(value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void Create_StringValueNode_3_With_Location()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");

        // act
        var value = new StringValueNode(location, stringValue, true);

        // assert
        Assert.Equal("abc", value.Value);
        Assert.True(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Same(location, value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void StringValueNode_Equals_To_Null()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");
        var value = new StringValueNode(location, stringValue, true);

        // act
        var equals = value.Equals(null);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void StringValueNode_Equals_To_Same()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");
        var value = new StringValueNode(location, stringValue, true);

        // act
        var equals = value.Equals(value);

        // assert
        Assert.True(equals);
    }

    [InlineData("abc", true)]
    [InlineData("def", false)]
    [Theory]
    public void StringValueNode_Equals_To_Other(string value, bool expected)
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var value1 = new StringValueNode(location, "abc", true);
        var value2 = new StringValueNode(location, value, true);

        // act
        var equals = value1.Equals(value2, Syntax);

        // assert
        Assert.Equal(expected, equals);
    }

    [Fact]
    public void ValueNode_Equals_To_Null()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");
        var value = new StringValueNode(location, stringValue, true);

        // act
        var equals = value.Equals((IValueNode)null!, Syntax);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void ValueNode_Equals_To_Same()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");
        var value = new StringValueNode(location, stringValue, true);

        // act
        var equals = value.Equals((IValueNode)value);

        // assert
        Assert.True(equals);
    }

    [InlineData("abc", true)]
    [InlineData("def", false)]
    [Theory]
    public void ValueNode_Equals_To_Other(string value, bool expected)
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var value1 = new StringValueNode(location, "abc", true);
        var value2 = new StringValueNode(location, value, true);

        // act
        var equals = value1.Equals((IValueNode)value2, Syntax);

        // assert
        Assert.Equal(expected, equals);
    }

    [Fact]
    public void ValueNode_Equals_To_Int()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var stringValue = Encoding.UTF8.GetBytes("abc");
        var value = new StringValueNode(location, stringValue, true);

        // act
        var equals = value.Equals(new IntValueNode(123), Syntax);

        // assert
        Assert.False(equals);
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var value = new StringValueNode("abc");

        // act
        value = value.WithLocation(location);

        // assert
        Assert.Equal("abc", value.Value);
        Assert.False(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Same(location, value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void WithValue_1()
    {
        // arrange
        var value = new StringValueNode("abc");

        // act
        value = value.WithValue("def");

        // assert
        Assert.Equal("def", value.Value);
        Assert.False(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Null(value.Location);
        Assert.Empty(value.GetNodes());
    }

    [Fact]
    public void WithValue_2()
    {
        // arrange
        var value = new StringValueNode("abc");
        Assert.False(value.Block);

        // act
        value = value.WithValue("def", true);

        // assert
        Assert.Equal("def", value.Value);
        Assert.True(value.Block);
        Assert.Equal(SyntaxKind.StringValue, value.Kind);
        Assert.Null(value.Location);
        Assert.Empty(value.GetNodes());
    }
}
