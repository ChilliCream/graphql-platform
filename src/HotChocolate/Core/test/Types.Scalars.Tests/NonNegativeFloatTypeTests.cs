using HotChocolate.Language;

namespace HotChocolate.Types;

public class NonNegativeFloatTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<NonNegativeFloatType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, true)]
    [InlineData(typeof(FloatValueNode), 0d, true)]
    [InlineData(typeof(FloatValueNode), double.MinValue, false)]
    [InlineData(typeof(FloatValueNode), double.MaxValue, true)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "foo", false)]
    [InlineData(typeof(IntValueNode), int.MinValue, false)]
    [InlineData(typeof(IntValueNode), -1, false)]
    [InlineData(typeof(IntValueNode), 0, true)]
    [InlineData(typeof(IntValueNode), 1, true)]
    [InlineData(typeof(IntValueNode), int.MaxValue, true)]
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
        ExpectIsInstanceOfTypeToMatch<NonNegativeFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, true)]
    [InlineData(double.MinValue, false)]
    [InlineData(double.MaxValue, true)]
    [InlineData(true, false)]
    [InlineData("foo", false)]
    [InlineData(int.MinValue, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(-1, false)]
    [InlineData(1, false)]
    [InlineData(0, false)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<NonNegativeFloatType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, 0d)]
    [InlineData(typeof(IntValueNode), 1, 1d)]
    [InlineData(typeof(FloatValueNode), double.MaxValue, double.MaxValue)]
    [InlineData(typeof(FloatValueNode), 1d, 1d)]
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
        ExpectParseLiteralToMatch<NonNegativeFloatType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), -1d)]
    [InlineData(typeof(FloatValueNode), double.MinValue)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(IntValueNode), int.MinValue)]
    [InlineData(typeof(IntValueNode), -1)]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<NonNegativeFloatType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(FloatValueNode), double.MaxValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<NonNegativeFloatType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(-1d)]
    [InlineData(double.MinValue)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<NonNegativeFloatType>(value);
    }

    [Theory]
    [InlineData(1d, 1d)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<NonNegativeFloatType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(-1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<PositiveIntType>(value);
    }

    [Theory]
    [InlineData(1d, 1d)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<NonNegativeFloatType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(-1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(double.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<NonNegativeFloatType>(value);
    }

    [Theory]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(FloatValueNode), double.MaxValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<NonNegativeFloatType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(-1d)]
    [InlineData(double.MinValue)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<NonNegativeFloatType>(value);
    }
}
