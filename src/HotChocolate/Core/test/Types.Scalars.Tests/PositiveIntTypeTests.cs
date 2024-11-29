using HotChocolate.Language;

namespace HotChocolate.Types;

public class PositiveIntTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<PositiveIntType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "foo", false)]
    [InlineData(typeof(IntValueNode), int.MinValue, false)]
    [InlineData(typeof(IntValueNode), -1, false)]
    [InlineData(typeof(IntValueNode), 0, false)]
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
        ExpectIsInstanceOfTypeToMatch<PositiveIntType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(true, false)]
    [InlineData("foo", false)]
    [InlineData(int.MinValue, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PositiveIntType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 1, 1)]
    [InlineData(typeof(IntValueNode), int.MaxValue, int.MaxValue)]
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
        ExpectParseLiteralToMatch<PositiveIntType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(IntValueNode), int.MinValue)]
    [InlineData(typeof(IntValueNode), -1)]
    [InlineData(typeof(IntValueNode), 0)]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<PositiveIntType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), int.MaxValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<PositiveIntType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
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
        ExpectParseValueToThrowSerializationException<PositiveIntType>(value);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<PositiveIntType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
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
    [InlineData(1, 1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<PositiveIntType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<PositiveIntType>(value);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), int.MaxValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<PositiveIntType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<PositiveIntType>(value);
    }
}
