using HotChocolate.Language;

namespace HotChocolate.Types;

public class RgbTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<RgbType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)", "rgb(255 0 153)")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<RgbType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "1")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153.6, 1)")]
    [InlineData(typeof(StringValueNode), "rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData(typeof(StringValueNode), "rgb(٢٥٥,٠,٠)")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<RgbType>(valueNode);
    }

    [Theory]
    [InlineData("\"rgb(255,0,0)\"", "rgb(255,0,0)")]
    [InlineData("\"rgb(100%, 0%, 0%)\"", "rgb(100%, 0%, 0%)")]
    [InlineData("\"rgb(300,0,0)\"", "rgb(300,0,0)")]
    [InlineData("\"rgb(255 0 153)\"", "rgb(255 0 153)")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<RgbType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("\"rgb(255, 0, 153.6, 1)\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<RgbType>(jsonValue);
    }

    [Theory]
    [InlineData("rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)")]
    [InlineData("rgb(300,0,0)")]
    [InlineData("rgb(255 0 153)")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<RgbType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<RgbType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<RgbType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<RgbType>(value);
    }
}
