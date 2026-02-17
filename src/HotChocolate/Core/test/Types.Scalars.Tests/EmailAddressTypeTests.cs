using HotChocolate.Language;

namespace HotChocolate.Types;

public class EmailAddressTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<EmailAddressType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "test@chillicream.com", "test@chillicream.com")]
    [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com", "CapitalizeTest@chillicream.com")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(Type type, object? value, object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<EmailAddressType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "invalid.email.com")]
    [InlineData(typeof(StringValueNode), "email@-example.com")]
    [InlineData(typeof(StringValueNode), "email@example..com")]
    [InlineData(typeof(StringValueNode), "\u212A@chillicream.com")] // U+212A = Kelvin Sign
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<EmailAddressType>(valueNode);
    }

    [Theory]
    [InlineData("\"test@chillicream.com\"", "test@chillicream.com")]
    [InlineData("\"CapitalizeTest@chillicream.com\"", "CapitalizeTest@chillicream.com")]
    public void CoerceInputValue_GivenValue_MatchExpected(string inputValue, object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<EmailAddressType>(inputValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"invalid.email.com\"")]
    [InlineData("\"email@-example.com\"")]
    [InlineData("\"email@example..com\"")]
    public void CoerceInputValue_GivenValue_Throw(string inputValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<EmailAddressType>(inputValue);
    }

    [Theory]
    [InlineData("test@chillicream.com")]
    [InlineData("CapitalizeTest@chillicream.com")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<EmailAddressType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("invalid.email.com")]
    [InlineData("email@-example.com")]
    [InlineData("email@example..com")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<EmailAddressType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "test@chillicream.com")]
    [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<EmailAddressType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("invalid.email.com")]
    [InlineData("email@-example.com")]
    [InlineData("email@example..com")]
    [InlineData("")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<EmailAddressType>(value);
    }
}
