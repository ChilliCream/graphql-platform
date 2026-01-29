using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class DecimalTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new DecimalType();

        // assert
        Assert.Equal("Decimal", type.Name);
    }

    [Fact]
    public void IsValueCompatible_FloatLiteral_True()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsValueCompatible(CreateExponentialLiteral());

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_IntLiteral_True()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsValueCompatible(new IntValueNode(123));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_StringLiteral_False()
    {
        // arrange
        var type = new DecimalType();

        // act
        var result = type.IsValueCompatible(new StringValueNode("123"));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = CreateFixedPointLiteral();

        // act
        var value = type.CoerceInputLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void CoerceInputLiteral_Exponential()
    {
        // arrange
        var type = new DecimalType();
        var literal = CreateExponentialLiteral();

        // act
        var value = type.CoerceInputLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void CoerceInputLiteral_IntLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = new IntValueNode(123);

        // act
        var value = type.CoerceInputLiteral(literal);

        // assert
        Assert.IsType<decimal>(value);
        Assert.Equal(literal.ToDecimal(), value);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new DecimalType();
        var literal = new StringValueNode("abc");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Null_Throws()
    {
        // arrange
        var type = new DecimalType();

        // act
        void Action() => type.CoerceInputLiteral(null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new DecimalType();
        var inputValue = JsonDocument.Parse("123.456").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(123.456m, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new DecimalType();
        var inputValue = JsonDocument.Parse("\"abc\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new DecimalType();
        const decimal runtimeValue = 123.456M;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("123.456");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new DecimalType();
        const string runtimeValue = "abc";

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue_MaxValue_Violation()
    {
        // arrange
        var type = new DecimalType(0, 100);
        const decimal runtimeValue = 123.456M;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new DecimalType();
        const decimal runtimeValue = 123.456M;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        var floatLiteral = Assert.IsType<FloatValueNode>(literal);
        Assert.Equal(runtimeValue, floatLiteral.ToDecimal());
    }

    [Fact]
    public void ValueToLiteral_MaxValue()
    {
        // arrange
        var type = new DecimalType(1, 100);
        const decimal input = 100M;

        // act
        var literal = type.ValueToLiteral(input);

        // assert
        Assert.Equal(100M, Assert.IsType<FloatValueNode>(literal).ToDecimal());
    }

    [Fact]
    public void ValueToLiteral_MaxValue_Violation()
    {
        // arrange
        var type = new DecimalType(1, 100);
        const decimal input = 101M;

        // act
        void Action() => type.ValueToLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral_MinValue()
    {
        // arrange
        var type = new DecimalType(1, 100);
        const decimal input = 1M;

        // act
        var literal = type.ValueToLiteral(input);

        // assert
        Assert.Equal(1M, Assert.IsType<FloatValueNode>(literal).ToDecimal());
    }

    [Fact]
    public void ValueToLiteral_MinValue_Violation()
    {
        // arrange
        var type = new DecimalType(1, 100);
        const decimal input = 0M;

        // act
        void Action() => type.ValueToLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new DecimalType();
        var literal = CreateFixedPointLiteral();

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(literal.ToDecimal(), Assert.IsType<decimal>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DecimalType();

        // act
        void Action() => type.CoerceInputLiteral(new StringValueNode("abc"));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
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
    public void ValueToLiteral_HandlesMoreThan6Digits()
    {
        // arrange
        var type = new DecimalType();
        const decimal input = 1234567.1234567m;
        const string output = "1234567.1234567";

        // act
        var result = type.ValueToLiteral(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ValueToLiteral_FormatsToDefaultSignificantDigits()
    {
        // arrange
        var type = new DecimalType();
        const decimal input = 1234567.891123456789m;
        const string output = "1234567.891123456789";

        // act
        var result = type.ValueToLiteral(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ValueToLiteral_Handle12Digits()
    {
        // arrange
        var type = new DecimalType();
        const decimal input = 1234567.890123456789m;
        const string output = "1234567.890123456789";

        // act
        var result = type.ValueToLiteral(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    [Fact]
    public void ValueToLiteral_FormatsToSpecifiedNumberOfDecimalDigitsLong()
    {
        // arrange
        var type = new DecimalType();
        const decimal input = 1234567.890123456789m;
        const string output = "1234567.890123456789";

        // act
        var result = type.ValueToLiteral(input);

        // assert
        Assert.True(result is FloatValueNode);
        Assert.True(result.Value is string);
        Assert.Equal(output, (string)result.Value);
    }

    private FloatValueNode CreateExponentialLiteral() =>
        new FloatValueNode(
            new ReadOnlyMemorySegment("1.000000E+000"u8.ToArray()),
            FloatFormat.Exponential);

    private FloatValueNode CreateFixedPointLiteral() =>
        new FloatValueNode(
            new ReadOnlyMemorySegment("1.23"u8.ToArray()),
            FloatFormat.FixedPoint);
}
