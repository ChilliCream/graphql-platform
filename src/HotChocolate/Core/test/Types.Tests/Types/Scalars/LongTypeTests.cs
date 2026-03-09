using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class LongTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new LongType();

        // assert
        Assert.Equal("Long", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new LongType();
        var literal = new IntValueNode(42L);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(42L, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MaxValue()
    {
        // arrange
        var type = new LongType();
        var literal = new IntValueNode(long.MaxValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(long.MaxValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MinValue()
    {
        // arrange
        var type = new LongType();
        var literal = new IntValueNode(long.MinValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(long.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new LongType();
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
        var type = new LongType();
        var inputValue = JsonDocument.Parse("42").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(42L, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_MaxValue()
    {
        // arrange
        var type = new LongType();
        var inputValue = JsonDocument.Parse($"{long.MaxValue}").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(long.MaxValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_MinValue()
    {
        // arrange
        var type = new LongType();
        var inputValue = JsonDocument.Parse($"{long.MinValue}").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(long.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new LongType();
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
        var type = new LongType();
        const long runtimeValue = 42;

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
        var type = new LongType();

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
        var type = new LongType();
        const long runtimeValue = 42;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(42L, Assert.IsType<IntValueNode>(literal).ToInt64());
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LongType();
        var literal = new IntValueNode(42L);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(42L, Assert.IsType<long>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new LongType();

        // act
        void Action() => type.CoerceInputLiteral(new FloatValueNode(1.5));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }
}
