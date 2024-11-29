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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "1", false)]
    [InlineData(typeof(StringValueNode), "1.2", false)]
    [InlineData(typeof(StringValueNode), "1.2.3", false)]
    [InlineData(typeof(StringValueNode), "1.2.3.4", true)]
    [InlineData(typeof(StringValueNode), "255.255.255.255", true)]
    [InlineData(typeof(StringValueNode), "255.256.256.256", false)]
    [InlineData(typeof(StringValueNode), "300.256.256.256", false)]
    [InlineData(typeof(StringValueNode), "255.300.256.256", false)]
    [InlineData(typeof(StringValueNode), "255.256.300.256", false)]
    [InlineData(typeof(StringValueNode), "255.256.256.300", false)]
    [InlineData(typeof(StringValueNode), "127.0.0.1", true)]
    [InlineData(typeof(StringValueNode), "192.168.0.1", true)]
    [InlineData(typeof(StringValueNode), "000.000.000.000", true)]
    [InlineData(typeof(StringValueNode), "00.00.00.00", true)]
    [InlineData(typeof(StringValueNode), "0.0.0.0/32", true)]
    [InlineData(typeof(StringValueNode), "000.000.000.000/32", true)]
    [InlineData(typeof(StringValueNode), "255.255.255.255/0", true)]
    [InlineData(typeof(StringValueNode), "255.255.255.255/33", false)]
    [InlineData(typeof(StringValueNode), "127.0.0.1/0", true)]
    [InlineData(typeof(StringValueNode), "192.168.2.1/0", true)]
    [InlineData(typeof(StringValueNode), "0.0.0.3/2", true)]
    [InlineData(typeof(StringValueNode), "0.0.0.127/7", true)]
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
        ExpectIsInstanceOfTypeToMatch<IPv4Type>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("1", false)]
    [InlineData("1.2", false)]
    [InlineData("1.2.3", false)]
    [InlineData("1.2.3.4", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("255.256.256.256", false)]
    [InlineData("300.256.256.256", false)]
    [InlineData("255.300.256.256", false)]
    [InlineData("255.256.300.256", false)]
    [InlineData("255.256.256.300", false)]
    [InlineData("127.0.0.1", true)]
    [InlineData("192.168.0.1", true)]
    [InlineData("000.000.000.000", true)]
    [InlineData("00.00.00.00", true)]
    [InlineData("0.0.0.0/32", true)]
    [InlineData("000.000.000.000/32", true)]
    [InlineData("255.255.255.255/0", true)]
    [InlineData("255.255.255.255/33", false)]
    [InlineData("127.0.0.1/0", true)]
    [InlineData("192.168.2.1/0", true)]
    [InlineData("0.0.0.3/2", true)]
    [InlineData("0.0.0.127/7", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<IPv4Type>(value, expected);
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
        ExpectParseLiteralToMatch<IPv4Type>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<IPv4Type>(valueNode);
    }

    [Theory]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("255.255.255.255", "255.255.255.255")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    [InlineData("192.168.0.1", "192.168.0.1")]
    [InlineData("000.000.000.000", "000.000.000.000")]
    [InlineData("00.00.00.00", "00.00.00.00")]
    [InlineData("0.0.0.0/32", "0.0.0.0/32")]
    [InlineData("000.000.000.000/32", "000.000.000.000/32")]
    [InlineData("255.255.255.255/0", "255.255.255.255/0")]
    [InlineData("127.0.0.1/0", "127.0.0.1/0")]
    [InlineData("192.168.2.1/0", "192.168.2.1/0")]
    [InlineData("0.0.0.3/2", "0.0.0.3/2")]
    [InlineData("0.0.0.127/7", "0.0.0.127/7")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<IPv4Type>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<IPv4Type>(value);
    }

    [Theory]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("255.255.255.255", "255.255.255.255")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    [InlineData("192.168.0.1", "192.168.0.1")]
    [InlineData("000.000.000.000", "000.000.000.000")]
    [InlineData("00.00.00.00", "00.00.00.00")]
    [InlineData("0.0.0.0/32", "0.0.0.0/32")]
    [InlineData("000.000.000.000/32", "000.000.000.000/32")]
    [InlineData("255.255.255.255/0", "255.255.255.255/0")]
    [InlineData("127.0.0.1/0", "127.0.0.1/0")]
    [InlineData("192.168.2.1/0", "192.168.2.1/0")]
    [InlineData("0.0.0.3/2", "0.0.0.3/2")]
    [InlineData("0.0.0.127/7", "0.0.0.127/7")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<IPv4Type>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<IPv4Type>(value);
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
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<IPv4Type>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<IPv4Type>(value);
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
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<IPv4Type>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
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
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<IPv4Type>(value);
    }
}
