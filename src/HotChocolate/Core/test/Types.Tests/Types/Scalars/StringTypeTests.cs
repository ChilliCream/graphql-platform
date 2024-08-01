using HotChocolate.Language;

namespace HotChocolate.Types;

public class StringTypeTests
{
    [Fact]
    public void EnsureStringTypeKindIsCorret()
    {
        // arrange
        var type = new StringType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    public void IsInstanceOfType_ValueNode()
    {
        // arrange
        var type = new StringType();
        var input = new StringValueNode("123456");

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullValueNode()
    {
        // arrange
        var type = new StringType();
        var input = NullValueNode.Default;

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_Wrong_ValueNode()
    {
        // arrange
        var type = new StringType();
        var input = new IntValueNode(123456);

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_Throws()
    {
        // arrange
        var type = new StringType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => type.IsInstanceOfType(null));
    }

    [Fact]
    public void Serialize_Type()
    {
        // arrange
        var type = new StringType();
        var input = "123456";

        // act
        var serializedValue = type.Serialize(input);

        // assert
        Assert.IsType<string>(serializedValue);
        Assert.Equal("123456", serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var type = new StringType();

        // act
        var serializedValue = type.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_Wrong_Type_Throws()
    {
        // arrange
        var type = new StringType();
        object input = 123456;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.Serialize(input));
    }

    [Fact]
    public void ParseLiteral_ValueNode()
    {
        // arrange
        var type = new StringType();
        var input = new StringValueNode("123456");

        // act
        var output = type.ParseLiteral(input);

        // assert
        Assert.IsType<string>(output);
        Assert.Equal("123456", output);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var type = new StringType();
        var input = NullValueNode.Default;

        // act
        var output = type.ParseLiteral(input);

        // assert
        Assert.Null(output);
    }

    [Fact]
    public void ParseLiteral_Wrong_ValueNode_Throws()
    {
        // arrange
        var type = new StringType();
        var input = new IntValueNode(123456);

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseLiteral(input));
    }

    [Fact]
    public void ParseLiteral_Null_Throws()
    {
        // arrange
        var type = new StringType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() => type.ParseLiteral(null));
    }

    [Fact]
    public void ParseValue_Wrong_Value_Throws()
    {
        // arrange
        var type = new StringType();
        object input = 123456;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseValue(input));
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var type = new StringType();
        object input = null;

        // act
        object output = type.ParseValue(input);

        // assert
        Assert.IsType<NullValueNode>(output);
    }
}
