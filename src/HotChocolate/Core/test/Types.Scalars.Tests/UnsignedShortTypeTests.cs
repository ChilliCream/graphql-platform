using HotChocolate.Language;

namespace HotChocolate.Types;

public class UnsignedShortTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<UnsignedShortType>();

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
    [InlineData(typeof(IntValueNode), 0, true)]
    [InlineData(typeof(IntValueNode), 1, true)]
    [InlineData(typeof(IntValueNode), ushort.MaxValue, true)]
    [InlineData(typeof(IntValueNode), ushort.MinValue, true)]
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
        ExpectIsInstanceOfTypeToMatch<UnsignedShortType>(valueNode, expected);
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
    [InlineData((ushort)1, true)]
    [InlineData(ushort.MaxValue, true)]
    [InlineData(ushort.MinValue, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<UnsignedShortType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, (ushort)0)]
    [InlineData(typeof(IntValueNode), 1, (ushort)1)]
    [InlineData(typeof(IntValueNode), ushort.MaxValue, ushort.MaxValue)]
    [InlineData(typeof(IntValueNode), ushort.MinValue, ushort.MinValue)]
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
        ExpectParseLiteralToMatch<UnsignedShortType>(valueNode, expected);
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
        ExpectParseLiteralToThrowSerializationException<UnsignedShortType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (ushort)1)]
    [InlineData(typeof(IntValueNode), ushort.MaxValue)]
    [InlineData(typeof(IntValueNode), ushort.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<UnsignedShortType>(value, type);
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
        ExpectParseValueToThrowSerializationException<UnsignedShortType>(value);
    }

    [Theory]
    [InlineData(0, (ushort)0)]
    [InlineData(1, (ushort)1)]
    [InlineData(ushort.MaxValue, ushort.MaxValue)]
    [InlineData(ushort.MinValue, ushort.MinValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<UnsignedShortType>(resultValue, runtimeValue);
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
        ExpectDeserializeToThrowSerializationException<UnsignedShortType>(value);
    }

    [Theory]
    [InlineData((ushort)0, (ushort)0)]
    [InlineData((ushort)1, (ushort)1)]
    [InlineData(ushort.MaxValue, ushort.MaxValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<UnsignedShortType>(runtimeValue, resultValue);
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
        ExpectSerializeToThrowSerializationException<UnsignedShortType>(value);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (ushort)1)]
    [InlineData(typeof(IntValueNode), ushort.MaxValue)]
    [InlineData(typeof(IntValueNode), ushort.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<UnsignedShortType>(value, type);
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
        ExpectParseResultToThrowSerializationException<UnsignedShortType>(value);
    }

    [Fact]
    public async Task UnsignedShortType_Should_BeBoundImplicity_When_Registered()
    {
        // arrange
        // act
        // assert
        await ExpectScalarTypeToBoundImplicityWhenRegistered<UnsignedShortType, DefaultUnsignedShort>();
    }

    public class DefaultUnsignedShort
    {
        public ushort Short { get; }
    }
}
