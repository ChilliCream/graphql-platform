using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class UnsignedLongTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new UnsignedLongType();

        // assert
        Assert.Equal("UnsignedLong", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new UnsignedLongType();
        var literal = new IntValueNode(42UL);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal((ulong)42, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MaxValue()
    {
        // arrange
        var type = new UnsignedLongType();
        var literal = new IntValueNode(ulong.MaxValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(ulong.MaxValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MinValue()
    {
        // arrange
        var type = new UnsignedLongType();
        var literal = new IntValueNode(ulong.MinValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(ulong.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new UnsignedLongType();
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
        var type = new UnsignedLongType();
        var inputValue = JsonDocument.Parse("42").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal((ulong)42, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_MaxValue()
    {
        // arrange
        var type = new UnsignedLongType();
        var inputValue = JsonDocument.Parse($"{ulong.MaxValue}").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(ulong.MaxValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_MinValue()
    {
        // arrange
        var type = new UnsignedLongType();
        var inputValue = JsonDocument.Parse($"{ulong.MinValue}").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(ulong.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new UnsignedLongType();
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
        var type = new UnsignedLongType();
        const ulong runtimeValue = 42;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("42");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new UnsignedLongType();

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
        var type = new UnsignedLongType();
        const ulong runtimeValue = 42;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(42UL, Assert.IsType<IntValueNode>(literal).ToUInt64());
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new UnsignedLongType();
        var literal = new IntValueNode(42UL);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal((ulong)42, Assert.IsType<ulong>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new UnsignedLongType();

        // act
        void Action() => type.CoerceInputLiteral(new FloatValueNode(1.5));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }
}
