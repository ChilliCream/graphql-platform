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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "K@chillicream.com", false)] // K = Kelvin Sign (U+212A)
    [InlineData(typeof(StringValueNode), "test@chillicream.com", true)]
    [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com", true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsValueCompatible_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<EmailAddressType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "test@chillicream.com", "test@chillicream.com")]
    [InlineData(typeof(StringValueNode),
        "CapitalizeTest@chillicream.com",
        "CapitalizeTest@chillicream.com")]
    [InlineData(typeof(NullValueNode), null, null)]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<EmailAddressType>(valueNode, expected);
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
    [InlineData(typeof(StringValueNode), "K@chillicream.com")] // K = Kelvin Sign (U+212A)
    public void CoerceInputLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<EmailAddressType>(valueNode);
    }

    [Theory]
    [InlineData("\"test@chillicream.com\"", "test@chillicream.com")]
    [InlineData("\"CapitalizeTest@chillicream.com\"", "CapitalizeTest@chillicream.com")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<EmailAddressType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"invalid.email.com\"")]
    [InlineData("\"email@-example.com\"")]
    [InlineData("\"email@example..com\"")]
    public void CoerceInputValue_GivenValue_ThrowSerializationException(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrowSerializationException<EmailAddressType>(jsonValue);
    }

    [Theory]
    [InlineData("test@chillicream.com")]
    [InlineData("CapitalizeTest@chillicream.com")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object? runtimeValue)
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
    public void CoerceOutputValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrowSerializationException<EmailAddressType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "test@chillicream.com")]
    [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com")]
    [InlineData(typeof(NullValueNode), null)]
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
