using HotChocolate.Language;

namespace HotChocolate.Types;

public class HslaTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<HslaType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode),
        "hsla(240, 100%, 50%, .05)",
        "hsla(240, 100%, 50%, .05)")]
    [InlineData(typeof(StringValueNode),
        "hsla(240, 100%, 50%, .4)",
        "hsla(240, 100%, 50%, .4)")]
    [InlineData(typeof(StringValueNode),
        "hsla(240, 100%, 50%, .7)",
        "hsla(240, 100%, 50%, .7)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, 1)", "hsla(240, 100%, 50%, 1)")]
    [InlineData(typeof(StringValueNode),
        "hsla(240 100% 50% / .05)",
        "hsla(240 100% 50% / .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / 5%)", "hsla(240 100% 50% / 5%)")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<HslaType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "hsla(090)")]
    [InlineData(typeof(StringValueNode), "hsla(#FFFFFF)")]
    [InlineData(typeof(StringValueNode), "hsla(FF, A5, 00, .2)")]
    [InlineData(typeof(StringValueNode), "hsla(240, FF, 50, 0.2)")]
    [InlineData(typeof(StringValueNode), "hsla(270%, A0, 5F, 1.0)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 75, .3, 25%)")]
    [InlineData(typeof(StringValueNode), "hsla(٢٤٠, ١٠٠%, ٥٠%, .٠٥)")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<HslaType>(valueNode);
    }

    [Theory]
    [InlineData("\"hsla(240, 100%, 50%, .05)\"", "hsla(240, 100%, 50%, .05)")]
    [InlineData("\"hsla(240, 100%, 50%, .4)\"", "hsla(240, 100%, 50%, .4)")]
    [InlineData("\"hsla(240, 100%, 50%, .7)\"", "hsla(240, 100%, 50%, .7)")]
    [InlineData("\"hsla(240, 100%, 50%, 1)\"", "hsla(240, 100%, 50%, 1)")]
    [InlineData("\"hsla(240 100% 50% / .05)\"", "hsla(240 100% 50% / .05)")]
    [InlineData("\"hsla(240 100% 50% / 5%)\"", "hsla(240 100% 50% / 5%)")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<HslaType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"hsla(090)\"")]
    [InlineData("\"hsla(#FFFFFF)\"")]
    [InlineData("\"hsla(FF, A5, 00, .2)\"")]
    [InlineData("\"hsla(240, FF, 50, 0.2)\"")]
    [InlineData("\"hsla(270%, A0, 5F, 1.0)\"")]
    [InlineData("\"hsla(240, 75, .3, 25%)\"")]
    [InlineData("\"hsla(٢٤٠, ١٠٠%, ٥٠%, .٠٥)\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<HslaType>(jsonValue);
    }

    [Theory]
    [InlineData("hsla(240, 100%, 50%, .05)")]
    [InlineData("hsla(240, 100%, 50%, .4)")]
    [InlineData("hsla(240, 100%, 50%, .7)")]
    [InlineData("hsla(240, 100%, 50%, 1)")]
    [InlineData("hsla(240 100% 50% / .05)")]
    [InlineData("hsla(240 100% 50% / 5%)")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<HslaType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    [InlineData("hsla(٢٤٠, ١٠٠%, ٥٠%, .٠٥)")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<HslaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .4)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .7)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, 1)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / 5%)")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<HslaType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    [InlineData("hsla(٢٤٠, ١٠٠%, ٥٠%, .٠٥)")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<HslaType>(value);
    }
}
