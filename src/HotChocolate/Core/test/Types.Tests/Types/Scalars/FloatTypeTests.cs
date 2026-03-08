using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class FloatTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new FloatType();

        // assert
        Assert.Equal("Float", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new FloatType();
        var literal = new FloatValueNode(42.5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(42.5, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_FixedPoint()
    {
        // arrange
        var type = new FloatType();
        var literal = CreateFixedPointLiteral();

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(literal.ToDouble(), runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Exponential()
    {
        // arrange
        var type = new FloatType();
        var literal = CreateExponentialLiteral();

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(literal.ToDouble(), runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_IntLiteral()
    {
        // arrange
        var type = new FloatType();
        var literal = new IntValueNode(42);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(42.0, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new FloatType();
        var literal = new StringValueNode("foo");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new FloatType();
        var inputValue = JsonDocument.Parse("42.5").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(42.5, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new FloatType();
        var inputValue = JsonDocument.Parse("\"foo\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new FloatType();
        const double runtimeValue = 42.5;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("42.5");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new FloatType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("foo", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new FloatType();
        const double runtimeValue = 42.5;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(42.5, Assert.IsType<FloatValueNode>(literal).ToDouble());
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new FloatType();
        var literal = new FloatValueNode(42.5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(42.5, Assert.IsType<double>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new FloatType();

        // act
        void Action() => type.CoerceInputLiteral(new StringValueNode("abc"));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
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
