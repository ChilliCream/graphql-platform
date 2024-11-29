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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)", true)]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)", true)]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)", true)]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)", true)]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)", true)]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)", true)]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)", true)]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 1)", true)]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 100%)", true)]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 1)", true)]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 100%)", true)]
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
        ExpectIsInstanceOfTypeToMatch<RgbType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("rgb(255,0,0)", true)]
    [InlineData("rgb(100%, 0%, 0%)", true)]
    [InlineData("rgb(300,0,0)", true)]
    [InlineData("rgb(110%, 0%, 0%)", true)]
    [InlineData("rgb(100%,0%,60%)", true)]
    [InlineData("rgb(100%, 0%, 60%)", true)]
    [InlineData("rgb(255 0 153)", true)]
    [InlineData("rgb(255, 0, 153, 1)", true)]
    [InlineData("rgb(255, 0, 153, 100%)", true)]
    [InlineData("rgb(255 0 153 / 1)", true)]
    [InlineData("rgb(255 0 153 / 100%)", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<RgbType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)", "rgb(255 0 153)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 1)", "rgb(255, 0, 153, 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 100%)", "rgb(255, 0, 153, 100%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 1)", "rgb(255 0 153 / 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 100%)", "rgb(255 0 153 / 100%)")]
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
        ExpectParseLiteralToMatch<RgbType>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<RgbType>(valueNode);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData("rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData("rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData("rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData("rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData("rgb(255 0 153)", "rgb(255 0 153)")]
    [InlineData("rgb(255, 0, 153, 1)", "rgb(255, 0, 153, 1)")]
    [InlineData("rgb(255, 0, 153, 100%)", "rgb(255, 0, 153, 100%)")]
    [InlineData("rgb(255 0 153 / 1)", "rgb(255 0 153 / 1)")]
    [InlineData("rgb(255 0 153 / 100%)", "rgb(255 0 153 / 100%)")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<RgbType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<RgbType>(value);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData("rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData("rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData("rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData("rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData("rgb(255 0 153)", "rgb(255 0 153)")]
    [InlineData("rgb(255, 0, 153, 1)", "rgb(255, 0, 153, 1)")]
    [InlineData("rgb(255, 0, 153, 100%)", "rgb(255, 0, 153, 100%)")]
    [InlineData("rgb(255 0 153 / 1)", "rgb(255 0 153 / 1)")]
    [InlineData("rgb(255 0 153 / 100%)", "rgb(255 0 153 / 100%)")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<RgbType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<RgbType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 100%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 100%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<RgbType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<RgbType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255, 0, 153, 100%)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 1)")]
    [InlineData(typeof(StringValueNode), "rgb(255 0 153 / 100%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<RgbType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(255, 0, 153.6, 1)")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<RgbType>(value);
    }
}
