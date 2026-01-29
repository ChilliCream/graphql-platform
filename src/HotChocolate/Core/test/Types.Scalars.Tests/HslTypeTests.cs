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
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)", "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)", "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)", "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)", "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)", "hsl(270, 100%, 50%)")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<HslType>(valueNode, expected);
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
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<HslType>(valueNode);
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
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<HslType>(jsonValue);
    }

    [Theory]
    [InlineData("hsl(270,60%,70%)")]
    [InlineData("hsl(270, 60%, 70%)")]
    [InlineData("hsl(270 60% 70%)")]
    [InlineData("hsl(270deg, 60%, 70%)")]
    [InlineData("hsl(270, 100%, 50%)")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
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
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<HslType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)")]
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
