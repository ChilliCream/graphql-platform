using HotChocolate.Language;

namespace HotChocolate.Types;

public class PostalCodeTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<PostalCodeType>();

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
    [InlineData(typeof(StringValueNode), "44256", true)]
    [InlineData(typeof(StringValueNode), "97909", true)]
    [InlineData(typeof(StringValueNode), "64200", true)]
    [InlineData(typeof(StringValueNode), "18000", true)]
    [InlineData(typeof(StringValueNode), "98020", true)]
    [InlineData(typeof(StringValueNode), "09070", true)]
    [InlineData(typeof(StringValueNode), "1000 AP", true)]
    [InlineData(typeof(StringValueNode), "48383", true)]
    [InlineData(typeof(StringValueNode), "52070", true)]
    [InlineData(typeof(StringValueNode), "2605", true)]
    [InlineData(typeof(StringValueNode), "6771", true)]
    [InlineData(typeof(StringValueNode), "114 55", true)]
    [InlineData(typeof(StringValueNode), "1060", true)]
    [InlineData(typeof(StringValueNode), "6560", true)]
    [InlineData(typeof(StringValueNode), "4881", true)]
    [InlineData(typeof(StringValueNode), "9485", true)]
    [InlineData(typeof(StringValueNode), "EC1A 1BB", true)]
    [InlineData(typeof(StringValueNode), "M1 1AE", true)]
    [InlineData(typeof(StringValueNode), "B2V 0A0", true)]
    [InlineData(typeof(StringValueNode), "V9E 9Z9", true)]
    [InlineData(typeof(StringValueNode), "3500", true)]
    [InlineData(typeof(StringValueNode), "0872", true)]
    [InlineData(typeof(StringValueNode), "110091", true)]
    [InlineData(typeof(StringValueNode), "4099", true)]
    [InlineData(typeof(StringValueNode), "1001", true)]
    [InlineData(typeof(StringValueNode), "7004", true)]
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
        ExpectIsInstanceOfTypeToMatch<PostalCodeType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("44256", true)]
    [InlineData("97909", true)]
    [InlineData("64200", true)]
    [InlineData("18000", true)]
    [InlineData("98020", true)]
    [InlineData("09070", true)]
    [InlineData("1000 AP", true)]
    [InlineData("48383", true)]
    [InlineData("52070", true)]
    [InlineData("2605", true)]
    [InlineData("6771", true)]
    [InlineData("114 55", true)]
    [InlineData("1060", true)]
    [InlineData("6560", true)]
    [InlineData("4881", true)]
    [InlineData("9485", true)]
    [InlineData("EC1A 1BB", true)]
    [InlineData("M1 1AE", true)]
    [InlineData("B2V 0A0", true)]
    [InlineData("V9E 9Z9", true)]
    [InlineData("3500", true)]
    [InlineData("0872", true)]
    [InlineData("110091", true)]
    [InlineData("4099", true)]
    [InlineData("1001", true)]
    [InlineData("7004", true)]
    [InlineData(null, true)]

    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PostalCodeType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "44256", "44256")]
    [InlineData(typeof(StringValueNode), "97909", "97909")]
    [InlineData(typeof(StringValueNode), "64200", "64200")]
    [InlineData(typeof(StringValueNode), "18000", "18000")]
    [InlineData(typeof(StringValueNode), "98020", "98020")]
    [InlineData(typeof(StringValueNode), "09070", "09070")]
    [InlineData(typeof(StringValueNode), "1000 AP", "1000 AP")]
    [InlineData(typeof(StringValueNode), "48383", "48383")]
    [InlineData(typeof(StringValueNode), "52070", "52070")]
    [InlineData(typeof(StringValueNode), "2605", "2605")]
    [InlineData(typeof(StringValueNode), "6771", "6771")]
    [InlineData(typeof(StringValueNode), "114 55", "114 55")]
    [InlineData(typeof(StringValueNode), "1060", "1060")]
    [InlineData(typeof(StringValueNode), "6560", "6560")]
    [InlineData(typeof(StringValueNode), "4881", "4881")]
    [InlineData(typeof(StringValueNode), "9485", "9485")]
    [InlineData(typeof(StringValueNode), "EC1A 1BB", "EC1A 1BB")]
    [InlineData(typeof(StringValueNode), "M1 1AE", "M1 1AE")]
    [InlineData(typeof(StringValueNode), "B2V 0A0", "B2V 0A0")]
    [InlineData(typeof(StringValueNode), "V9E 9Z9", "V9E 9Z9")]
    [InlineData(typeof(StringValueNode), "3500", "3500")]
    [InlineData(typeof(StringValueNode), "0872", "0872")]
    [InlineData(typeof(StringValueNode), "110091", "110091")]
    [InlineData(typeof(StringValueNode), "4099", "4099")]
    [InlineData(typeof(StringValueNode), "1001", "1001")]
    [InlineData(typeof(StringValueNode), "7004", "7004")]
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
        ExpectParseLiteralToMatch<PostalCodeType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "XYZ")]
    [InlineData(typeof(StringValueNode), "XYZ ZZ")]
    [InlineData(typeof(StringValueNode), "XYZ 12")]
    [InlineData(typeof(StringValueNode), "XYZ 123")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<PostalCodeType>(valueNode);
    }

    [Theory]
    [InlineData("44256", "44256")]
    [InlineData("97909", "97909")]
    [InlineData("64200", "64200")]
    [InlineData("18000", "18000")]
    [InlineData("98020", "98020")]
    [InlineData("09070", "09070")]
    [InlineData("1000 AP", "1000 AP")]
    [InlineData("48383", "48383")]
    [InlineData("52070", "52070")]
    [InlineData("2605", "2605")]
    [InlineData("6771", "6771")]
    [InlineData("114 55", "114 55")]
    [InlineData("1060", "1060")]
    [InlineData("6560", "6560")]
    [InlineData("4881", "4881")]
    [InlineData("9485", "9485")]
    [InlineData("EC1A 1BB", "EC1A 1BB")]
    [InlineData("M1 1AE", "M1 1AE")]
    [InlineData("B2V 0A0", "B2V 0A0")]
    [InlineData("V9E 9Z9", "V9E 9Z9")]
    [InlineData("3500", "3500")]
    [InlineData("0872", "0872")]
    [InlineData("110091", "110091")]
    [InlineData("4099", "4099")]
    [InlineData("1001", "1001")]
    [InlineData("7004", "7004")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<PostalCodeType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("")]
    [InlineData("XYZ")]
    [InlineData("XYZ ZZ")]
    [InlineData("XYZ 12")]
    [InlineData("XYZ 123")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<PostalCodeType>(value);
    }

    [Theory]
    [InlineData("44256", "44256")]
    [InlineData("97909", "97909")]
    [InlineData("64200", "64200")]
    [InlineData("18000", "18000")]
    [InlineData("98020", "98020")]
    [InlineData("09070", "09070")]
    [InlineData("1000 AP", "1000 AP")]
    [InlineData("48383", "48383")]
    [InlineData("52070", "52070")]
    [InlineData("2605", "2605")]
    [InlineData("6771", "6771")]
    [InlineData("114 55", "114 55")]
    [InlineData("1060", "1060")]
    [InlineData("6560", "6560")]
    [InlineData("4881", "4881")]
    [InlineData("9485", "9485")]
    [InlineData("EC1A 1BB", "EC1A 1BB")]
    [InlineData("M1 1AE", "M1 1AE")]
    [InlineData("B2V 0A0", "B2V 0A0")]
    [InlineData("V9E 9Z9", "V9E 9Z9")]
    [InlineData("3500", "3500")]
    [InlineData("0872", "0872")]
    [InlineData("110091", "110091")]
    [InlineData("4099", "4099")]
    [InlineData("1001", "1001")]
    [InlineData("7004", "7004")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<PostalCodeType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("XYZ")]
    [InlineData("XYZ ZZ")]
    [InlineData("XYZ 12")]
    [InlineData("XYZ 123")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<PostalCodeType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "44256")]
    [InlineData(typeof(StringValueNode), "97909")]
    [InlineData(typeof(StringValueNode), "64200")]
    [InlineData(typeof(StringValueNode), "18000")]
    [InlineData(typeof(StringValueNode), "98020")]
    [InlineData(typeof(StringValueNode), "09070")]
    [InlineData(typeof(StringValueNode), "1000 AP")]
    [InlineData(typeof(StringValueNode), "48383")]
    [InlineData(typeof(StringValueNode), "52070")]
    [InlineData(typeof(StringValueNode), "2605")]
    [InlineData(typeof(StringValueNode), "6771")]
    [InlineData(typeof(StringValueNode), "114 55")]
    [InlineData(typeof(StringValueNode), "1060")]
    [InlineData(typeof(StringValueNode), "6560")]
    [InlineData(typeof(StringValueNode), "4881")]
    [InlineData(typeof(StringValueNode), "9485")]
    [InlineData(typeof(StringValueNode), "EC1A 1BB")]
    [InlineData(typeof(StringValueNode), "M1 1AE")]
    [InlineData(typeof(StringValueNode), "B2V 0A0")]
    [InlineData(typeof(StringValueNode), "V9E 9Z9")]
    [InlineData(typeof(StringValueNode), "3500")]
    [InlineData(typeof(StringValueNode), "0872")]
    [InlineData(typeof(StringValueNode), "110091")]
    [InlineData(typeof(StringValueNode), "4099")]
    [InlineData(typeof(StringValueNode), "1001")]
    [InlineData(typeof(StringValueNode), "7004")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<PostalCodeType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("XYZ")]
    [InlineData("XYZ ZZ")]
    [InlineData("XYZ 12")]
    [InlineData("XYZ 123")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<PostalCodeType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "44256")]
    [InlineData(typeof(StringValueNode), "97909")]
    [InlineData(typeof(StringValueNode), "64200")]
    [InlineData(typeof(StringValueNode), "18000")]
    [InlineData(typeof(StringValueNode), "98020")]
    [InlineData(typeof(StringValueNode), "09070")]
    [InlineData(typeof(StringValueNode), "1000 AP")]
    [InlineData(typeof(StringValueNode), "48383")]
    [InlineData(typeof(StringValueNode), "52070")]
    [InlineData(typeof(StringValueNode), "2605")]
    [InlineData(typeof(StringValueNode), "6771")]
    [InlineData(typeof(StringValueNode), "114 55")]
    [InlineData(typeof(StringValueNode), "1060")]
    [InlineData(typeof(StringValueNode), "6560")]
    [InlineData(typeof(StringValueNode), "4881")]
    [InlineData(typeof(StringValueNode), "9485")]
    [InlineData(typeof(StringValueNode), "EC1A 1BB")]
    [InlineData(typeof(StringValueNode), "M1 1AE")]
    [InlineData(typeof(StringValueNode), "B2V 0A0")]
    [InlineData(typeof(StringValueNode), "V9E 9Z9")]
    [InlineData(typeof(StringValueNode), "3500")]
    [InlineData(typeof(StringValueNode), "0872")]
    [InlineData(typeof(StringValueNode), "110091")]
    [InlineData(typeof(StringValueNode), "4099")]
    [InlineData(typeof(StringValueNode), "1001")]
    [InlineData(typeof(StringValueNode), "7004")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<PostalCodeType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("XYZ")]
    [InlineData("XYZ ZZ")]
    [InlineData("XYZ 12")]
    [InlineData("XYZ 123")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<PostalCodeType>(value);
    }
}
