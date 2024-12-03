using HotChocolate.Language;

namespace HotChocolate.Types;

public class UnsignedLongTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<UnsignedLongType>();

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
    [InlineData(typeof(IntValueNode), 1UL, true)]
    [InlineData(typeof(IntValueNode), ulong.MinValue, true)]
    [InlineData(typeof(IntValueNode), ulong.MaxValue, true)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<UnsignedLongType>(valueNode, expected);
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
    [InlineData((ulong)1, true)]
    [InlineData(ulong.MinValue, true)]
    [InlineData(ulong.MaxValue, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<UnsignedLongType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, (ulong)0)]
    [InlineData(typeof(IntValueNode), 1, (ulong)1)]
    [InlineData(typeof(IntValueNode), ulong.MaxValue, ulong.MaxValue)]
    [InlineData(typeof(IntValueNode), ulong.MinValue, ulong.MinValue)]
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
        ExpectParseLiteralToMatch<UnsignedLongType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<UnsignedLongType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (ulong)1)]
    [InlineData(typeof(IntValueNode), ulong.MaxValue)]
    [InlineData(typeof(IntValueNode), ulong.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<UnsignedLongType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<UnsignedIntType>(value);
    }

    [Theory]
    [InlineData(1UL, 1UL)]
    [InlineData(ulong.MaxValue, ulong.MaxValue)]
    [InlineData(ulong.MinValue, ulong.MinValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<UnsignedLongType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<UnsignedLongType>(value);
    }

    [Theory]
    [InlineData((ulong)0, (ulong)0)]
    [InlineData((ulong)1, (ulong)1)]
    [InlineData(ulong.MaxValue, ulong.MaxValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<UnsignedLongType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<UnsignedLongType>(value);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (ulong)1)]
    [InlineData(typeof(IntValueNode), ulong.MaxValue)]
    [InlineData(typeof(IntValueNode), ulong.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<UnsignedLongType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<UnsignedLongType>(value);
    }

    [Fact]
    public async Task UnsignedLongType_Should_BeBoundImplicity_When_Registered()
    {
        // arrange
        // act
        // assert
        await ExpectScalarTypeToBoundImplicityWhenRegistered<UnsignedLongType, DefaultUnsignedLongType>();
    }

    public class DefaultUnsignedLongType
    {
        public ulong Long { get; }
    }
}
