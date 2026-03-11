using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class BooleanTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new BooleanType();

        // assert
        Assert.Equal("Boolean", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new BooleanType();
        var literal = new BooleanValueNode(null, true);

        // act
        var result = type.CoerceInputLiteral(literal);

        // assert
        Assert.IsType<bool>(result);
        Assert.True((bool)result!);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new BooleanType();
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
        var type = new BooleanType();
        var inputValue = JsonDocument.Parse("true").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(true, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new BooleanType();
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
        var type = new BooleanType();
        const bool runtimeValue = true;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("true");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new BooleanType();

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
        var type = new BooleanType();
        const bool runtimeValue = true;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.True(Assert.IsType<BooleanValueNode>(literal).Value);
    }

    [Fact]
    public void IsValueCompatible_BooleanLiteral_True()
    {
        // arrange
        var type = new BooleanType();
        var literal = new BooleanValueNode(null, true);

        // act
        var result = type.IsValueCompatible(literal);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_StringLiteral_False()
    {
        // arrange
        var type = new BooleanType();
        var literal = new StringValueNode(null, "12345", false);

        // act
        var result = type.IsValueCompatible(literal);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new BooleanType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new BooleanType();
        var literal = new BooleanValueNode(null, true);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.True(Assert.IsType<bool>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new BooleanType();

        // act
        void Action() => type.CoerceInputLiteral(new StringValueNode("abc"));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }
}
