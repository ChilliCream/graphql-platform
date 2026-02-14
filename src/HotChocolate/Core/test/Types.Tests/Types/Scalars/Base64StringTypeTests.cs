using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class Base64StringTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new Base64StringType();

        // assert
        Assert.Equal("Base64String", type.Name);
    }

    [Fact]
    public void IsValueCompatible_StringLiteral_True()
    {
        // arrange
        var type = new Base64StringType();
        var literal = new StringValueNode("abc");

        // act
        var isOfType = type.IsValueCompatible(literal);

        // assert
        Assert.True(isOfType);
    }

    [Fact]
    public void IsValueCompatible_IntLiteral_False()
    {
        // arrange
        var type = new Base64StringType();
        var literal = new IntValueNode(123);

        // act
        var isOfType = type.IsValueCompatible(literal);

        // assert
        Assert.False(isOfType);
    }

    [Fact]
    public void IsValueCompatible_Null_ReturnsFalse()
    {
        // arrange
        var type = new Base64StringType();

        // act
        void Error() => type.IsValueCompatible(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new Base64StringType();
        var expected = "value"u8.ToArray();
        var literal = new StringValueNode(Convert.ToBase64String(expected));

        // act
        var actual = (byte[]?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new Base64StringType();
        var literal = new IntValueNode(123);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Null_Throws()
    {
        // arrange
        var type = new Base64StringType();

        // act
        void Action() => type.CoerceInputLiteral(null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new Base64StringType();
        var bytes = "value"u8.ToArray();
        var inputValue = JsonDocument.Parse($"\"{Convert.ToBase64String(bytes)}\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(bytes, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new Base64StringType();
        var inputValue = JsonDocument.Parse("123").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new Base64StringType();
        var value = "value"u8.ToArray();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(value, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"dmFsdWU=\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new Base64StringType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(123, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new Base64StringType();
        var expected = "value"u8.ToArray();
        var expectedLiteralValue = Convert.ToBase64String(expected);

        // act
        var stringLiteral = (StringValueNode)type.ValueToLiteral(expected);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new Base64StringType();
        var expected = "value"u8.ToArray();
        var literal = new StringValueNode(Convert.ToBase64String(expected));

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, Assert.IsType<byte[]>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new Base64StringType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new Base64StringType();

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }
}
