using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class UnsignedByteTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new UnsignedByteType();

        // assert
        Assert.Equal("UnsignedByte", type.Name);
    }

    [Fact]
    public void IsValueCompatible_IntLiteral_True()
    {
        // arrange
        var type = new UnsignedByteType();
        var literal = new IntValueNode(1);

        // act
        var result = type.IsValueCompatible(literal);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_FloatLiteral_False()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        var result = type.IsValueCompatible(new FloatValueNode(1M));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsValueCompatible_Null_False()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        var result = type.IsValueCompatible(null!);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new UnsignedByteType();
        var literal = new IntValueNode(1);

        // act
        var value = type.CoerceInputLiteral(literal);

        // assert
        Assert.IsType<byte>(value);
        Assert.Equal(literal.ToByte(), value);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new UnsignedByteType();
        var input = new StringValueNode("abc");

        // act
        void Action() => type.CoerceInputLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Null_Throws()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        void Action() => type.CoerceInputLiteral(null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new UnsignedByteType();
        var inputValue = JsonDocument.Parse("123").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal((byte)123, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new UnsignedByteType();
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
        var type = new UnsignedByteType();
        const byte runtimeValue = 123;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("123");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("foo", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue_MaxValue_Violation()
    {
        // arrange
        var type = new UnsignedByteType(0, 100);
        const byte value = 200;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(value, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new UnsignedByteType();
        const byte runtimeValue = 123;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(123, Assert.IsType<IntValueNode>(literal).ToByte());
    }

    [Fact]
    public void ValueToLiteral_MaxValue()
    {
        // arrange
        var type = new UnsignedByteType(1, 100);
        const byte input = 100;

        // act
        var literal = (IntValueNode)type.ValueToLiteral(input);

        // assert
        Assert.Equal(100, literal.ToByte());
    }

    [Fact]
    public void ValueToLiteral_MaxValue_Violation()
    {
        // arrange
        var type = new UnsignedByteType(1, 100);
        const byte input = 101;

        // act
        void Action() => type.ValueToLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral_MinValue()
    {
        // arrange
        var type = new UnsignedByteType(1, 100);
        const byte input = 1;

        // act
        var literal = (IntValueNode)type.ValueToLiteral(input);

        // assert
        Assert.Equal(1, literal.ToByte());
    }

    [Fact]
    public void ValueToLiteral_MinValue_Violation()
    {
        // arrange
        var type = new UnsignedByteType(1, 100);
        const byte input = 0;

        // act
        void Action() => type.ValueToLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new UnsignedByteType();
        var literal = new IntValueNode(123);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal((byte)123, Assert.IsType<byte>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        void Action() => type.CoerceInputLiteral(new FloatValueNode(1.5));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new UnsignedByteType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }
}
