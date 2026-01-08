using HotChocolate.Language;

namespace HotChocolate.Types;

public class HslTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<HslType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "hsl(٢٧٠,٦٠%,٧٠%)", false)]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, .15)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, 15%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / .15)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / 15%)", true)]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)", true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsValueCompatible_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<HslType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)", "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)", "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)", "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)", "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, .15)", "hsl(270, 60%, 50%, .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, 15%)", "hsl(270, 60%, 50%, 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / .15)", "hsl(270 60% 50% / .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / 15%)", "hsl(270 60% 50% / 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)", "hsl(270, 100%, 50%)")]
    [InlineData(typeof(NullValueNode), null, null)]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<HslType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "hsl(090)")]
    [InlineData(typeof(StringValueNode), "hsl(#FFFFFF)")]
    [InlineData(typeof(StringValueNode), "hsl(FF, A5, 00)")]
    [InlineData(typeof(StringValueNode), "hsl(270, FF, 50)")]
    [InlineData(typeof(StringValueNode), "hsl(270%, A0, 5F)")]
    [InlineData(typeof(StringValueNode), "hsl(٢٧٠, ٦٠%, ٧٠%)")]
    public void CoerceInputLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<HslType>(valueNode);
    }

    [Theory]
    [InlineData("\"hsl(270,60%,70%)\"", "hsl(270,60%,70%)")]
    [InlineData("\"hsl(270, 60%, 70%)\"", "hsl(270, 60%, 70%)")]
    [InlineData("\"hsl(270 60% 70%)\"", "hsl(270 60% 70%)")]
    [InlineData("\"hsl(270deg, 60%, 70%)\"", "hsl(270deg, 60%, 70%)")]
    [InlineData("\"hsl(270, 100%, 50%)\"", "hsl(270, 100%, 50%)")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<HslType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"hsl(090)\"")]
    [InlineData("\"hsl(#FFFFFF)\"")]
    [InlineData("\"hsl(FF, A5, 00)\"")]
    [InlineData("\"hsl(270, FF, 50)\"")]
    public void CoerceInputValue_GivenValue_ThrowSerializationException(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrowSerializationException<HslType>(jsonValue);
    }

    [Theory]
    [InlineData("hsl(270,60%,70%)")]
    [InlineData("hsl(270, 60%, 70%)")]
    [InlineData("hsl(270 60% 70%)")]
    [InlineData("hsl(270deg, 60%, 70%)")]
    [InlineData("hsl(270, 100%, 50%)")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<HslType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    public void CoerceOutputValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrowSerializationException<HslType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<HslType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<HslType>(value);
    }
}
