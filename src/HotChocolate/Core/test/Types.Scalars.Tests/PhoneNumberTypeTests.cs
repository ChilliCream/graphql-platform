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
    [InlineData(typeof(StringValueNode), "+16873271234", "+16873271234")]
    [InlineData(typeof(StringValueNode),
        "+76543678901234",
        "+76543678901234")]
    [InlineData(typeof(StringValueNode),
        "+178955512343598",
        "+178955512343598")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<PhoneNumberType>(valueNode, expected);
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
    [InlineData(typeof(StringValueNode), "+1٧٨٩٥٥٥١٢٣٤")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<PhoneNumberType>(valueNode);
    }

    [Theory]
    [InlineData("\"+16873271234\"", "+16873271234")]
    [InlineData("\"+76543678901234\"", "+76543678901234")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<PhoneNumberType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"+765436789012345678901234\"")]
    [InlineData("\"765436789012345678901234\"")]
    [InlineData("\"(123)-456-7890\"")]
    [InlineData("\"123-456-7890\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<PhoneNumberType>(jsonValue);
    }

    [Theory]
    [InlineData("+16873271234")]
    [InlineData("+76543678901234")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<PhoneNumberType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<PhoneNumberType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "+16873271234")]
    [InlineData(typeof(StringValueNode), "+76543678901234")]
    [InlineData(typeof(StringValueNode), "+178955512343598")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<PhoneNumberType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("+765436789012345678901234")]
    [InlineData("765436789012345678901234")]
    [InlineData("(123)-456-7890")]
    [InlineData("123-456-7890")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<PhoneNumberType>(value);
    }
}
