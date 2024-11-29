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
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)", true)]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)", true)]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)", true)]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1) ", true)]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4) ", true)]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)", true)]
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
        ExpectIsInstanceOfTypeToMatch<RgbaType>(valueNode, expected);
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
    [InlineData("rgba(51, 170, 51, .1)", true)]
    [InlineData("rgba(51, 170, 51, .4)", true)]
    [InlineData("rgba(51, 170, 51, .7)", true)]
    [InlineData("rgba(51, 170, 51,  1)", true)]
    [InlineData("rgba(51 170 51 / 0.4)", true)]
    [InlineData("rgba(51 170 51 / 40%)", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<RgbaType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)", "rgba(51, 170, 51, .1)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)", "rgba(51, 170, 51, .4)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)", "rgba(51, 170, 51, .7)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1)", "rgba(51, 170, 51,  1)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4)", "rgba(51 170 51 / 0.4)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)", "rgba(51 170 51 / 40%)")]
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
        ExpectParseLiteralToMatch<RgbaType>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<RgbaType>(valueNode);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData("rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData("rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData("rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData("rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData("rgba(51, 170, 51, .1)", "rgba(51, 170, 51, .1)")]
    [InlineData("rgba(51, 170, 51, .4)", "rgba(51, 170, 51, .4)")]
    [InlineData("rgba(51, 170, 51, .7)", "rgba(51, 170, 51, .7)")]
    [InlineData("rgba(51, 170, 51,  1)", "rgba(51, 170, 51,  1)")]
    [InlineData("rgba(51 170 51 / 0.4)", "rgba(51 170 51 / 0.4)")]
    [InlineData("rgba(51 170 51 / 40%)", "rgba(51 170 51 / 40%)")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<RgbaType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<RgbaType>(value);
    }

    [Theory]
    [InlineData("rgb(255,0,0)", "rgb(255,0,0)")]
    [InlineData("rgb(100%, 0%, 0%)", "rgb(100%, 0%, 0%)")]
    [InlineData("rgb(300,0,0)", "rgb(300,0,0)")]
    [InlineData("rgb(110%, 0%, 0%)", "rgb(110%, 0%, 0%)")]
    [InlineData("rgb(100%,0%,60%)", "rgb(100%,0%,60%)")]
    [InlineData("rgb(100%, 0%, 60%)", "rgb(100%, 0%, 60%)")]
    [InlineData("rgba(51, 170, 51, .1)", "rgba(51, 170, 51, .1)")]
    [InlineData("rgba(51, 170, 51, .4)", "rgba(51, 170, 51, .4)")]
    [InlineData("rgba(51, 170, 51, .7)", "rgba(51, 170, 51, .7)")]
    [InlineData("rgba(51, 170, 51,  1)", "rgba(51, 170, 51,  1)")]
    [InlineData("rgba(51 170 51 / 0.4)", "rgba(51 170 51 / 0.4)")]
    [InlineData("rgba(51 170 51 / 40%)", "rgba(51 170 51 / 40%)")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<RgbaType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<RgbaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<RgbaType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<RgbaType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "rgb(255,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(300,0,0)")]
    [InlineData(typeof(StringValueNode), "rgb(110%, 0%, 0%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%,0%,60%)")]
    [InlineData(typeof(StringValueNode), "rgb(100%, 0%, 60%)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .1)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .4)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51, .7)")]
    [InlineData(typeof(StringValueNode), "rgba(51, 170, 51,  1)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 0.4)")]
    [InlineData(typeof(StringValueNode), "rgba(51 170 51 / 40%)")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<RgbaType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("rgb(1e2, .5e1, .5e0, +.25e2%)")]
    [InlineData("rgba(255, 0, 153.6, 1)")]
    [InlineData("rgba(1e2, .5e1, .5e0, +.25e2%)")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<RgbaType>(value);
    }
}
