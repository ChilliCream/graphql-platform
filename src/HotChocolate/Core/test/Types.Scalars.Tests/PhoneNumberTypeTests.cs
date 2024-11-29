using HotChocolate.Language;

namespace HotChocolate.Types;

public class PhoneNumberTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<PhoneNumberType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(NullValueNode), null, true)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "+17895551234", true)]
    [InlineData(typeof(StringValueNode), "+178955512343", true)]
    [InlineData(typeof(StringValueNode), "+1789555123435", true)]
    [InlineData(typeof(StringValueNode), "+178955512343598", true)]
    [InlineData(typeof(StringValueNode), "+765436789012345678901234", false)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PhoneNumberType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData(null, true)]
    [InlineData("+16873271234", true)]
    [InlineData("+765436789012", true)]
    [InlineData("+7654367890123", true)]
    [InlineData("+76543678901234", true)]
    [InlineData("+765436789012345", true)]
    [InlineData("+765436789012345678901234", false)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PhoneNumberType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "+16873271234", "+16873271234")]
    [InlineData(typeof(StringValueNode),
        "+76543678901234",
        "+76543678901234")]
    [InlineData(typeof(StringValueNode),
        "+178955512343598",
        "+178955512343598")]
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
        ExpectParseLiteralToMatch<PhoneNumberType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "+765436789012345678901234")]
    [InlineData(typeof(StringValueNode), "765436789012345678901234")]
    [InlineData(typeof(StringValueNode), "(123)-456-7890")]
    [InlineData(typeof(StringValueNode), "123-456-7890")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<PhoneNumberType>(valueNode);
    }

    [Theory]
    [InlineData("+16873271234", "+16873271234")]
    [InlineData("+76543678901234", "+76543678901234")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<PhoneNumberType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<PhoneNumberType>(value);
    }

    [Theory]
    [InlineData("+16873271234", "+16873271234")]
    [InlineData("+76543678901234", "+76543678901234")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<PhoneNumberType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<PhoneNumberType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "+16873271234")]
    [InlineData(typeof(StringValueNode), "+76543678901234")]
    [InlineData(typeof(StringValueNode), "+178955512343598")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<PhoneNumberType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<PhoneNumberType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "+16873271234")]
    [InlineData(typeof(StringValueNode), "+76543678901234")]
    [InlineData(typeof(StringValueNode), "+178955512343598")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<PhoneNumberType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<PhoneNumberType>(value);
    }
}
