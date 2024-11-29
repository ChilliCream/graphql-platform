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
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
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
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData(null, true)]
    [InlineData("hsl(270,60%,70%)", true)]
    [InlineData("hsl(270, 60%, 70%)", true)]
    [InlineData("hsl(270 60% 70%)", true)]
    [InlineData("hsl(270deg, 60%, 70%)", true)]
    [InlineData("hsl(270, 60%, 50%, .15)", true)]
    [InlineData("hsl(270, 60%, 50%, 15%)", true)]
    [InlineData("hsl(270 60% 50% / .15)", true)]
    [InlineData("hsl(270 60% 50% / 15%)", true)]
    [InlineData("hsl(270, 100%, 50%)", true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<HslType>(value, expected);
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
    public void ParseLiteral_GivenValueNode_MatchExpected(
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<HslType>(valueNode);
    }

    [Theory]
    [InlineData("hsl(270,60%,70%)", "hsl(270,60%,70%)")]
    [InlineData("hsl(270, 60%, 70%)", "hsl(270, 60%, 70%)")]
    [InlineData("hsl(270 60% 70%)", "hsl(270 60% 70%)")]
    [InlineData("hsl(270deg, 60%, 70%)", "hsl(270deg, 60%, 70%)")]
    [InlineData("hsl(270, 60%, 50%, .15)", "hsl(270, 60%, 50%, .15)")]
    [InlineData("hsl(270, 60%, 50%, 15%)", "hsl(270, 60%, 50%, 15%)")]
    [InlineData("hsl(270 60% 50% / .15)", "hsl(270 60% 50% / .15)")]
    [InlineData("hsl(270 60% 50% / 15%)", "hsl(270 60% 50% / 15%)")]
    [InlineData("hsl(270, 100%, 50%)", "hsl(270, 100%, 50%)")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<HslType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    [InlineData("hsl(270%, A0, 5F)")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<HslType>(value);
    }

    [Theory]
    [InlineData("hsl(270,60%,70%)", "hsl(270,60%,70%)")]
    [InlineData("hsl(270, 60%, 70%)", "hsl(270, 60%, 70%)")]
    [InlineData("hsl(270 60% 70%)", "hsl(270 60% 70%)")]
    [InlineData("hsl(270deg, 60%, 70%)", "hsl(270deg, 60%, 70%)")]
    [InlineData("hsl(270, 60%, 50%, .15)", "hsl(270, 60%, 50%, .15)")]
    [InlineData("hsl(270, 60%, 50%, 15%)", "hsl(270, 60%, 50%, 15%)")]
    [InlineData("hsl(270 60% 50% / .15)", "hsl(270 60% 50% / .15)")]
    [InlineData("hsl(270 60% 50% / 15%)", "hsl(270 60% 50% / 15%)")]
    [InlineData("hsl(270, 100%, 50%)", "hsl(270, 100%, 50%)")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<HslType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    [InlineData("hsl(270%, A0, 5F)")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<HslType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<HslType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    [InlineData("hsl(270%, A0, 5F)")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<HslType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "hsl(270,60%,70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270deg, 60%, 70%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 60%, 50%, 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / .15)")]
    [InlineData(typeof(StringValueNode), "hsl(270 60% 50% / 15%)")]
    [InlineData(typeof(StringValueNode), "hsl(270, 100%, 50%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<HslType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("hsl(090)")]
    [InlineData("hsl(#FFFFFF)")]
    [InlineData("hsl(FF, A5, 00)")]
    [InlineData("hsl(270, FF, 50)")]
    [InlineData("hsl(270%, A0, 5F)")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<HslType>(value);
    }
}
