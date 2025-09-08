using HotChocolate.Language;

namespace HotChocolate.Types;

public class NonPositiveFloatTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<NonPositiveFloatType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(FloatValueNode), -1.0d, true)]
    [InlineData(typeof(FloatValueNode), 0d, true)]
    [InlineData(typeof(FloatValueNode), 0.00000001d, false)]
    [InlineData(typeof(FloatValueNode), -0.0000001d, true)]
    [InlineData(typeof(FloatValueNode), double.MinValue, true)]
    [InlineData(typeof(IntValueNode), -1, true)]
    [InlineData(typeof(IntValueNode), int.MinValue, true)]
    [InlineData(typeof(IntValueNode), 0, true)]
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
        ExpectIsInstanceOfTypeToMatch<NonPositiveFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(1d, false)]
    [InlineData(-1d, true)]
    [InlineData(0.00000001d, false)]
    [InlineData(-0.0000001d, true)]
    [InlineData(double.MinValue, true)]
    [InlineData(0d, true)]
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
        ExpectIsInstanceOfTypeToMatch<NonPositiveFloatType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, 0d)]
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
        ExpectParseLiteralToMatch<NonPositiveFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<NonPositiveFloatType>(valueNode);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("")]
    public void ParseLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<NonPositiveFloatType>(value);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), 0d)]
    [InlineData(typeof(FloatValueNode), -12d)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<NonPositiveFloatType>(value, type);
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
        ExpectDeserializeToMatch<NonPositiveFloatType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1)]
    [InlineData(1d)]
    [InlineData(int.MaxValue)]
    [InlineData(double.MaxValue)]
    [InlineData(true)]
    [InlineData("foo")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<NonPositiveFloatType>(value);
    }

    [Theory]
    [InlineData(0d, 0d)]
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
        ExpectSerializeToMatch<NonPositiveFloatType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MaxValue)]
    [InlineData(double.MaxValue)]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<NonPositiveFloatType>(value);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<NonPositiveFloatType>(value);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), 0d)]
    [InlineData(typeof(FloatValueNode), -12d)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<NonPositiveFloatType>(value, type);
    }
}
