using HotChocolate.Language;

namespace HotChocolate.Types;

public class IPv6TypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<IPv6Type>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "2001:db8::7/32", "2001:db8::7/32")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/13", "a:b:c:d:e::1.2.3.4/13")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/64", "a:b:c:d:e::1.2.3.4/64")]
    [InlineData(typeof(StringValueNode),
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData(typeof(StringValueNode),
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData(typeof(StringValueNode),
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData(typeof(StringValueNode),
        "1080:0:0:0:8:800:200C:417A/27",
        "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData(typeof(StringValueNode), "2001:db8::7", "2001:db8::7")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4", "a:b:c:d:e::1.2.3.4")]
    [InlineData(typeof(StringValueNode),
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210")]
    [InlineData(typeof(StringValueNode),
        "1080:0:0:0:8:800:200C:417A",
        "1080:0:0:0:8:800:200C:417A")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6:7", "::1:2:3:4:5:6:7")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6", "::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5:6", "1::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5", "::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5", "1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4:5", "2:1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4", "::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4", "1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4", "2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3:4", "3:2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "::1:2:3", "::1:2:3")]
    [InlineData(typeof(StringValueNode), "1::1:2:3", "1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3", "2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::", "2:1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3", "3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2:3", "4:3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "::1:2", "::1:2")]
    [InlineData(typeof(StringValueNode), "1::1:2", "1::1:2")]
    [InlineData(typeof(StringValueNode), "2:1::1:2", "2:1::1:2")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2", "3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2", "4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1:2", "5:4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "::1", "::1")]
    [InlineData(typeof(StringValueNode), "1::1", "1::1")]
    [InlineData(typeof(StringValueNode), "2:1::1", "2:1::1")]
    [InlineData(typeof(StringValueNode), "3:2:1::1", "3:2:1::1")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1", "4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1", "5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::1", "6:5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "::", "::")]
    [InlineData(typeof(StringValueNode), "1::", "1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::", "3:2:1::")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::", "4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::", "5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::", "6:5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "7:6:5:4:3:2:1::", "7:6:5:4:3:2:1::")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<IPv6Type>(valueNode, expected);
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
    [InlineData(typeof(StringValueNode), "19.117.63.126")]
    [InlineData(typeof(StringValueNode), "300.256.256.256")]
    [InlineData(typeof(StringValueNode), "255.300.256.256")]
    [InlineData(typeof(StringValueNode), "255.256.300.256")]
    [InlineData(typeof(StringValueNode), "255.256.256.300")]
    [InlineData(typeof(StringValueNode), "255.255.255.255/33")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.22:100")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3..4/13")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<IPv6Type>(valueNode);
    }

    [Theory]
    [InlineData("\"2001:db8::7/32\"", "2001:db8::7/32")]
    [InlineData("\"a:b:c:d:e::1.2.3.4/13\"", "a:b:c:d:e::1.2.3.4/13")]
    [InlineData("\"a:b:c:d:e::1.2.3.4/64\"", "a:b:c:d:e::1.2.3.4/64")]
    [InlineData("\"FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0\"",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData("\"FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32\"",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData("\"FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128\"",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData("\"1080:0:0:0:8:800:200C:417A/27\"", "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData("\"2001:db8::7\"", "2001:db8::7")]
    [InlineData("\"a:b:c:d:e::1.2.3.4\"", "a:b:c:d:e::1.2.3.4")]
    [InlineData("\"::1\"", "::1")]
    [InlineData("\"::\"", "::")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<IPv6Type>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"1\"")]
    [InlineData("\"1.2\"")]
    [InlineData("\"1.2.3\"")]
    [InlineData("\"19.117.63.126\"")]
    [InlineData("\"300.256.256.256\"")]
    [InlineData("\"a:b:c:d:e::1.2.3.22:100\"")]
    [InlineData("\"a:b:c:d:e::1.2.3..4/13\"")]
    [InlineData("\"FEDC:BA98:7654:3210FEDC:BA98:7654:3210\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<IPv6Type>(jsonValue);
    }

    [Theory]
    [InlineData("2001:db8::7/32")]
    [InlineData("a:b:c:d:e::1.2.3.4/13")]
    [InlineData("a:b:c:d:e::1.2.3.4/64")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData("1080:0:0:0:8:800:200C:417A/27")]
    [InlineData("2001:db8::7")]
    [InlineData("a:b:c:d:e::1.2.3.4")]
    [InlineData("::1")]
    [InlineData("::")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<IPv6Type>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<IPv6Type>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/13")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/64")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData(typeof(StringValueNode), "2001:db8::7")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4")]
    [InlineData(typeof(StringValueNode), "::1")]
    [InlineData(typeof(StringValueNode), "::")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<IPv6Type>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<IPv6Type>(value);
    }
}
