using HotChocolate.Language;

namespace HotChocolate.Types;

public class ByteTypeTests
{
    [Fact]
    public void IsInstanceOfType_FloatLiteral_True()
    {
        // arrange
        var type = new ByteType();
        var literal = new IntValueNode(1);

        // act
        var result = type.IsInstanceOfType(literal);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullLiteral_True()
    {
        // arrange
        var type = new ByteType();

        // act
        var result = type.IsInstanceOfType(NullValueNode.Default);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_StringLiteral_False()
    {
        // arrange
        var type = new ByteType();

        // act
        var result = type.IsInstanceOfType(new FloatValueNode(1M));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_Throws()
    {
        // arrange
        var type = new ByteType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => type.IsInstanceOfType(null));
    }

    [Fact]
    public void Serialize_Type()
    {
        // arrange
        var type = new ByteType();
        byte value = 123;

        // act
        var serializedValue = type.Serialize(value);

        // assert
        Assert.IsType<byte>(serializedValue);
        Assert.Equal(value, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var type = new ByteType();

        // act
        var serializedValue = type.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_Wrong_Type_Throws()
    {
        // arrange
        var type = new ByteType();
        var input = "abc";

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.Serialize(input));
    }

    [Fact]
    public void Serialize_MaxValue_Violation()
    {
        // arrange
        var type = new ByteType(0, 100);
        byte value = 200;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.Serialize(value));
    }

    [Fact]
    public void ParseLiteral_IntLiteral()
    {
        // arrange
        var type = new ByteType();
        var literal = new IntValueNode(1);

        // act
        var value = type.ParseLiteral(literal);

        // assert
        Assert.IsType<byte>(value);
        Assert.Equal(literal.ToByte(), value);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var type = new ByteType();

        // act
        var output = type.ParseLiteral(NullValueNode.Default);

        // assert
        Assert.Null(output);
    }

    [Fact]
    public void ParseLiteral_Wrong_ValueNode_Throws()
    {
        // arrange
        var type = new ByteType();
        var input = new StringValueNode("abc");

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseLiteral(input));
    }

    [Fact]
    public void ParseLiteral_Null_Throws()
    {
        // arrange
        var type = new ByteType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => type.ParseLiteral(null));
    }

    [Fact]
    public void ParseValue_MaxValue()
    {
        // arrange
        var type = new ByteType(1, 100);
        byte input = 100;

        // act
        var literal = (IntValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(100, literal.ToByte());
    }

    [Fact]
    public void ParseValue_MaxValue_Violation()
    {
        // arrange
        var type = new ByteType(1, 100);
        byte input = 101;

        // act
        Action action = () => type.ParseValue(input);

        // assert
        Assert.Throws<SerializationException>(action);
    }

    [Fact]
    public void ParseValue_MinValue()
    {
        // arrange
        var type = new ByteType(1, 100);
        byte input = 1;

        // act
        var literal = (IntValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(1, literal.ToByte());
    }

    [Fact]
    public void ParseValue_MinValue_Violation()
    {
        // arrange
        var type = new ByteType(1, 100);
        byte input = 0;

        // act
        Action action = () => type.ParseValue(input);

        // assert
        Assert.Throws<SerializationException>(action);
    }

    [Fact]
    public void ParseValue_Wrong_Value_Throws()
    {
        // arrange
        var type = new ByteType();
        var value = "123";

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseValue(value));
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var type = new ByteType();
        object input = null;

        // act
        object output = type.ParseValue(input);

        // assert
        Assert.IsType<NullValueNode>(output);
    }

    [Fact]
    public void ParseValue_Nullable()
    {
        // arrange
        var type = new ByteType();
        byte? input = 123;

        // act
        var output = (IntValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(123, output.ToDouble());
    }

    [Fact]
    public void Ensure_TypeKind_is_Scalar()
    {
        // arrange
        var type = new ByteType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }
}
