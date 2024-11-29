using HotChocolate.Language;

namespace HotChocolate.Types;

public class SignedByteTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<IntType>();

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
    [InlineData(typeof(IntValueNode), sbyte.MaxValue, true)]
    [InlineData(typeof(IntValueNode), sbyte.MinValue, true)]
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
        ExpectIsInstanceOfTypeToMatch<SignedByteType>(valueNode, expected);
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
    [InlineData((sbyte)1, true)]
    [InlineData(sbyte.MaxValue, true)]
    [InlineData(sbyte.MinValue, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<SignedByteType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, (sbyte)0)]
    [InlineData(typeof(IntValueNode), 1, (sbyte)1)]
    [InlineData(typeof(IntValueNode), sbyte.MaxValue, sbyte.MaxValue)]
    [InlineData(typeof(IntValueNode), sbyte.MinValue, sbyte.MinValue)]
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
        ExpectParseLiteralToMatch<SignedByteType>(valueNode, expected);
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
        ExpectParseLiteralToThrowSerializationException<SignedByteType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (sbyte)1)]
    [InlineData(typeof(IntValueNode), sbyte.MaxValue)]
    [InlineData(typeof(IntValueNode), sbyte.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<SignedByteType>(value, type);
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
        ExpectParseValueToThrowSerializationException<SignedByteType>(value);
    }

    [Theory]
    [InlineData(0, (sbyte)0)]
    [InlineData(1, (sbyte)1)]
    [InlineData(sbyte.MaxValue, sbyte.MaxValue)]
    [InlineData(sbyte.MinValue, sbyte.MinValue)]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<SignedByteType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<SignedByteType>(value);
    }

    [Theory]
    [InlineData((sbyte)0, (sbyte)0)]
    [InlineData((sbyte)1, (sbyte)1)]
    [InlineData(sbyte.MaxValue, sbyte.MaxValue)]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<SignedByteType>(runtimeValue, resultValue);
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
        ExpectSerializeToThrowSerializationException<SignedByteType>(value);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), (sbyte)1)]
    [InlineData(typeof(IntValueNode), sbyte.MaxValue)]
    [InlineData(typeof(IntValueNode), sbyte.MinValue)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<SignedByteType>(value, type);
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
        ExpectParseResultToThrowSerializationException<SignedByteType>(value);
    }

    [Fact]
    public async Task SignedByteType_Should_BeBoundImplicity_When_Registered()
    {
        // arrange
        // act
        // assert
        await ExpectScalarTypeToBoundImplicityWhenRegistered<SignedByteType, DefaultSignedByte>();
    }

    public class DefaultSignedByte
    {
        public sbyte Byte { get; }
    }
}
