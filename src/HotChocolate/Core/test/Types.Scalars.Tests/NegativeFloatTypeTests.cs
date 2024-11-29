using HotChocolate.Language;

namespace HotChocolate.Types;

public class NegativeFloatTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<NegativeFloatType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(FloatValueNode), -1.0d, true)]
    [InlineData(typeof(FloatValueNode), 0d, false)]
    [InlineData(typeof(FloatValueNode), 0.00000001d, false)]
    [InlineData(typeof(FloatValueNode), -0.0000001d, true)]
    [InlineData(typeof(FloatValueNode), double.MinValue, true)]
    [InlineData(typeof(IntValueNode), -1, true)]
    [InlineData(typeof(IntValueNode), int.MinValue, true)]
    [InlineData(typeof(IntValueNode), 0, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "foo", false)]
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
        ExpectIsInstanceOfTypeToMatch<NegativeFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(1d, false)]
    [InlineData(-1d, true)]
    [InlineData(0.00000001d, false)]
    [InlineData(-0.0000001d, true)]
    [InlineData(double.MinValue, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("foo", false)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<NegativeFloatType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), -12, -12d)]
    [InlineData(typeof(FloatValueNode), -12.0, -12.0)]
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
        ExpectParseLiteralToMatch<NegativeFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 0)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<NegativeFloatType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), -1d)]
    [InlineData(typeof(FloatValueNode), -0.0000001d)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<NegativeFloatType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<NegativeFloatType>(value);
    }

    [Theory]
    [InlineData(-1d, -1d)]
    [InlineData(double.MinValue, double.MinValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<NegativeFloatType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    [InlineData(0)]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<NegativeFloatType>(value);
    }

    [Theory]
    [InlineData(-1d, -1d)]
    [InlineData(double.MinValue, double.MinValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<NegativeFloatType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MaxValue)]
    [InlineData(1)]
    [InlineData(0)]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<NegativeFloatType>(value);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), -1d)]
    [InlineData(typeof(FloatValueNode), -0.0000001d)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<NegativeFloatType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<NegativeFloatType>(value);
    }
}
