using HotChocolate.Language;

namespace HotChocolate.Types;

public class IPv4TypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<IPv4Type>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "1.2.3.4", "1.2.3.4")]
    [InlineData(typeof(StringValueNode), "255.255.255.255", "255.255.255.255")]
    [InlineData(typeof(StringValueNode), "127.0.0.1", "127.0.0.1")]
    [InlineData(typeof(StringValueNode), "192.168.0.1", "192.168.0.1")]
    [InlineData(typeof(StringValueNode), "000.000.000.000", "000.000.000.000")]
    [InlineData(typeof(StringValueNode), "00.00.00.00", "00.00.00.00")]
    [InlineData(typeof(StringValueNode), "0.0.0.0/32", "0.0.0.0/32")]
    [InlineData(typeof(StringValueNode), "000.000.000.000/32", "000.000.000.000/32")]
    [InlineData(typeof(StringValueNode), "255.255.255.255/0", "255.255.255.255/0")]
    [InlineData(typeof(StringValueNode), "127.0.0.1/0", "127.0.0.1/0")]
    [InlineData(typeof(StringValueNode), "192.168.2.1/0", "192.168.2.1/0")]
    [InlineData(typeof(StringValueNode), "0.0.0.3/2", "0.0.0.3/2")]
    [InlineData(typeof(StringValueNode), "0.0.0.127/7", "0.0.0.127/7")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<IPv4Type>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "1")]
    [InlineData(typeof(StringValueNode), "1.2")]
    [InlineData(typeof(StringValueNode), "1.2.3")]
    [InlineData(typeof(StringValueNode), "300.256.256.256")]
    [InlineData(typeof(StringValueNode), "255.300.256.256")]
    [InlineData(typeof(StringValueNode), "255.256.300.256")]
    [InlineData(typeof(StringValueNode), "255.256.256.300")]
    [InlineData(typeof(StringValueNode), "255.255.255.255/33")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<IPv4Type>(valueNode);
    }

    [Theory]
    [InlineData("\"1.2.3.4\"", "1.2.3.4")]
    [InlineData("\"255.255.255.255\"", "255.255.255.255")]
    [InlineData("\"127.0.0.1\"", "127.0.0.1")]
    [InlineData("\"192.168.0.1\"", "192.168.0.1")]
    [InlineData("\"000.000.000.000\"", "000.000.000.000")]
    [InlineData("\"00.00.00.00\"", "00.00.00.00")]
    [InlineData("\"0.0.0.0/32\"", "0.0.0.0/32")]
    [InlineData("\"000.000.000.000/32\"", "000.000.000.000/32")]
    [InlineData("\"255.255.255.255/0\"", "255.255.255.255/0")]
    [InlineData("\"127.0.0.1/0\"", "127.0.0.1/0")]
    [InlineData("\"192.168.2.1/0\"", "192.168.2.1/0")]
    [InlineData("\"0.0.0.3/2\"", "0.0.0.3/2")]
    [InlineData("\"0.0.0.127/7\"", "0.0.0.127/7")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<IPv4Type>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("\"1.2\"")]
    [InlineData("\"1.2.3\"")]
    [InlineData("\"300.256.256.256\"")]
    [InlineData("\"255.300.256.256\"")]
    [InlineData("\"255.256.300.256\"")]
    [InlineData("\"255.256.256.300\"")]
    [InlineData("\"255.255.255.255/33\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<IPv4Type>(jsonValue);
    }

    [Theory]
    [InlineData("1.2.3.4")]
    [InlineData("255.255.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.1")]
    [InlineData("000.000.000.000")]
    [InlineData("00.00.00.00")]
    [InlineData("0.0.0.0/32")]
    [InlineData("000.000.000.000/32")]
    [InlineData("255.255.255.255/0")]
    [InlineData("127.0.0.1/0")]
    [InlineData("192.168.2.1/0")]
    [InlineData("0.0.0.3/2")]
    [InlineData("0.0.0.127/7")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<IPv4Type>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<IPv4Type>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "1.2.3.4")]
    [InlineData(typeof(StringValueNode), "255.255.255.255")]
    [InlineData(typeof(StringValueNode), "127.0.0.1")]
    [InlineData(typeof(StringValueNode), "192.168.0.1")]
    [InlineData(typeof(StringValueNode), "000.000.000.000")]
    [InlineData(typeof(StringValueNode), "00.00.00.00")]
    [InlineData(typeof(StringValueNode), "0.0.0.0/32")]
    [InlineData(typeof(StringValueNode), "000.000.000.000/32")]
    [InlineData(typeof(StringValueNode), "255.255.255.255/0")]
    [InlineData(typeof(StringValueNode), "127.0.0.1/0")]
    [InlineData(typeof(StringValueNode), "192.168.2.1/0")]
    [InlineData(typeof(StringValueNode), "0.0.0.3/2")]
    [InlineData(typeof(StringValueNode), "0.0.0.127/7")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<IPv4Type>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<IPv4Type>(value);
    }
}
