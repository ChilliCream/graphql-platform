using HotChocolate.Language;

namespace HotChocolate.Types;

public class RgbaTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<RgbaType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)", "rgba(51, 170, 51, .1)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)", "rgba(51, 170, 51, .4)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)", "rgba(51, 170, 51, .7)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1)", "rgba(51, 170, 51,  1)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4)", "rgba(51 170 51 / 0.4)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)", "rgba(51 170 51 / 40%)")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<RgbaType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "1")]
    [InlineData(typeof(StringValueNode), "rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData(typeof(StringValueNode), "rgba(255, 0, 153.6, 1)")]
    [InlineData(typeof(StringValueNode), "rgba(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData(typeof(StringValueNode), "rgba(٥١, ١٧٠, ٥١, .١)")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<RgbaType>(valueNode);
    }

    [Theory]
    [InlineData("\"rgba(51, 170, 51, .1)\"", "rgba(51, 170, 51, .1)")]
    [InlineData("\"rgba(51, 170, 51, .4)\"", "rgba(51, 170, 51, .4)")]
    [InlineData("\"rgba(51, 170, 51, .7)\"", "rgba(51, 170, 51, .7)")]
    [InlineData("\"rgba(51, 170, 51,  1)\"", "rgba(51, 170, 51,  1)")]
    [InlineData("\"rgba(51 170 51 / 0.4)\"", "rgba(51 170 51 / 0.4)")]
    [InlineData("\"rgba(51 170 51 / 40%)\"", "rgba(51 170 51 / 40%)")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<RgbaType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"rgb(1e2, .5e1, .5e0, +.25e2%)\"")]
    [InlineData("\"rgba(255, 0, 153.6, 1)\"")]
    [InlineData("\"rgba(1e2, .5e1, .5e0, +.25e2%)\"")]
    [InlineData("\"rgba(٥١, ١٧٠, ٥١, .١)\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<RgbaType>(jsonValue);
    }

    [Theory]
    [InlineData("rgba(51, 170, 51, .1)")]
    [InlineData("rgba(51, 170, 51, .4)")]
    [InlineData("rgba(51, 170, 51, .7)")]
    [InlineData("rgba(51, 170, 51,  1)")]
    [InlineData("rgba(51 170 51 / 0.4)")]
    [InlineData("rgba(51 170 51 / 40%)")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<RgbaType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(٥١, ١٧٠, ٥١, .١)")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<RgbaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<RgbaType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(٥١, ١٧٠, ٥١, .١)")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<RgbaType>(value);
    }
}
