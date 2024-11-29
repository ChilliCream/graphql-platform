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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .05)", true)]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .4)", true)]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .7)", true)]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, 1)", true)]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / .05)", true)]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / 5%)", true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<HslaType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData(null, true)]
    [InlineData("hsla(240, 100%, 50%, .05)", true)]
    [InlineData("hsla(240, 100%, 50%, .4)", true)]
    [InlineData("hsla(240, 100%, 50%, .7)", true)]
    [InlineData("hsla(240, 100%, 50%, 1)", true)]
    [InlineData("hsla(240 100% 50% / .05)", true)]
    [InlineData("hsla(240 100% 50% / 5%)", true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<HslaType>(value, expected);
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
    [InlineData(typeof(NullValueNode), null, null)]
    public void ParseLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<HslaType>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<HslaType>(valueNode);
    }

    [Theory]
    [InlineData("hsla(240, 100%, 50%, .05)", "hsla(240, 100%, 50%, .05)")]
    [InlineData("hsla(240, 100%, 50%, .4)", "hsla(240, 100%, 50%, .4)")]
    [InlineData("hsla(240, 100%, 50%, .7)", "hsla(240, 100%, 50%, .7)")]
    [InlineData("hsla(240, 100%, 50%, 1)", "hsla(240, 100%, 50%, 1)")]
    [InlineData("hsla(240 100% 50% / .05)", "hsla(240 100% 50% / .05)")]
    [InlineData("hsla(240 100% 50% / 5%)", "hsla(240 100% 50% / 5%)")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<HslaType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<HslaType>(value);
    }

    [Theory]
    [InlineData("hsla(240, 100%, 50%, .05)", "hsla(240, 100%, 50%, .05)")]
    [InlineData("hsla(240, 100%, 50%, .4)", "hsla(240, 100%, 50%, .4)")]
    [InlineData("hsla(240, 100%, 50%, .7)", "hsla(240, 100%, 50%, .7)")]
    [InlineData("hsla(240, 100%, 50%, 1)", "hsla(240, 100%, 50%, 1)")]
    [InlineData("hsla(240 100% 50% / .05)", "hsla(240 100% 50% / .05)")]
    [InlineData("hsla(240 100% 50% / 5%)", "hsla(240 100% 50% / 5%)")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<HslaType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<HslaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .4)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .7)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, 1)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / 5%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<HslaType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<HslaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .4)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, .7)")]
    [InlineData(typeof(StringValueNode), "hsla(240, 100%, 50%, 1)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / .05)")]
    [InlineData(typeof(StringValueNode), "hsla(240 100% 50% / 5%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<HslaType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsla(090)")]
    [InlineData("hsla(#FFFFFF)")]
    [InlineData("hsla(FF, A5, 00, .2)")]
    [InlineData("hsla(240, FF, 50, 0.2)")]
    [InlineData("hsla(270%, A0, 5F, 1.0)")]
    [InlineData("hsla(240, 75, .3, 25%)")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<HslaType>(value);
    }
}
