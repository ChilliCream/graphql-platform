using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types;

public class DecimalTypeTests
{
    [Fact]
    public void IsInstanceOfType_FloatLiteral_True()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsInstanceOfType(CreateExponentialLiteral());

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullLiteral_True()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsInstanceOfType(NullValueNode.Default);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_IntLiteral_True()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsInstanceOfType(new IntValueNode(123));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_StringLiteral_False()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsInstanceOfType(new StringValueNode("123"));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_Throws()
    {
        // arrange
        var type = new DecimalType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => type.IsInstanceOfType(null));
    }

    [Fact]
    public void Serialize_Type()
    {
        // arrange
        var type = new DecimalType();
        var value = 123.456M;

        // act
        var serializedValue = type.Serialize(value);

        // assert
        Assert.IsType<decimal>(serializedValue);
        Assert.Equal(value, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var type = new DecimalType();

        // act
        var serializedValue = type.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_Wrong_Type_Throws()
    {
        // arrange
        var type = new DecimalType();
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
        var type = new DecimalType(0, 100);
        var value = 123.456M;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.Serialize(value));
    }

    [Fact]
    public void ParseLiteral_FixedPointLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = CreateFixedPointLiteral();

        // act
        var value = type.ParseLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void ParseLiteral_ExponentialLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = CreateExponentialLiteral();

        // act
        var value = type.ParseLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void ParseLiteral_IntLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = new IntValueNode(123);

        // act
        var value = type.ParseLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var type = new DecimalType();

        // act
        var output = type.ParseLiteral(NullValueNode.Default);

        // assert
        Assert.Null(output);
    }

    [Fact]
    public void ParseLiteral_Wrong_ValueNode_Throws()
    {
        // arrange
        var type = new DecimalType();
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
        var type = new DecimalType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => type.ParseLiteral(null));
    }

    [Fact]
    public void ParseValue_MaxValue()
    {
        // arrange
        var type = new DecimalType(1, 100);
        var input = 100M;

        // act
        var literal = (FloatValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(100M, literal.ToDecimal());
    }

    [Fact]
    public void ParseValue_MaxValue_Violation()
    {
        // arrange
        var type = new DecimalType(1, 100);
        var input = 101M;

        // act
        Action action = () => type.ParseValue(input);

        // assert
        Assert.Throws<SerializationException>(action);
    }

    [Fact]
    public void ParseValue_MinValue()
    {
        // arrange
        var type = new DecimalType(1, 100);
        var input = 1M;

        // act
        var literal = (FloatValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(1M, literal.ToDecimal());
    }

    [Fact]
    public void ParseValue_MinValue_Violation()
    {
        // arrange
        var type = new DecimalType(1, 100);
        var input = 0M;

        // act
        Action action = () => type.ParseValue(input);

        // assert
        Assert.Throws<SerializationException>(action);
    }

    [Fact]
    public void ParseValue_Wrong_Value_Throws()
    {
        // arrange
        var type = new DecimalType();
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
        var type = new DecimalType();
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
        var type = new DecimalType();
        decimal? input = 123M;

        // act
        var output = (FloatValueNode)type.ParseValue(input);

        // assert
        Assert.Equal(123M, output.ToDecimal());
    }

    [Fact]
    public void Ensure_TypeKind_is_Scalar()
    {
        // arrange
        var type = new DecimalType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void ParseValue_HandlesMoreThan6Digits()
    {
        // arrange
        var type = new DecimalType();
        var input = 1234567.1234567m;
        var output = "1234567.1234567";

        // act
        var result = type.ParseValue(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ParseValue_FormatsToDefaultSignificantDigits()
    {
        // arrange
        var type = new DecimalType();
        var input = 1234567.891123456789m;
        var output = "1234567.891123456789";

        // act
        var result = type.ParseValue(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ParseValue_Handle12Digits()
    {
        // arrange
        var type = new DecimalType();
        var input = 1234567.890123456789m;
        var output = "1234567.890123456789";

        // act
        var result = type.ParseValue(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ParseValue_FormatsToSpecifiedNumberOfDecimalDigitsLong()
    {
        // arrange
        var type = new DecimalType();
        var input = 1234567.890123456789m;
        var output = "1234567.890123456789";

        // act
        var result = type.ParseValue(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    private FloatValueNode CreateExponentialLiteral() =>
        new FloatValueNode(Encoding.UTF8.GetBytes("1.000000E+000"), FloatFormat.Exponential);

    private FloatValueNode CreateFixedPointLiteral() =>
        new FloatValueNode(Encoding.UTF8.GetBytes("1.23"), FloatFormat.FixedPoint);
}
