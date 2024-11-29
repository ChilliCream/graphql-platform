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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "1", false)]
    [InlineData(typeof(StringValueNode), "1.2", false)]
    [InlineData(typeof(StringValueNode), "1.2.3", false)]
    [InlineData(typeof(StringValueNode), "2001:db8::7/32", true)]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/13", true)]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/64", true)]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0", true)]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32", true)]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128", true)]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A/27", true)]
    [InlineData(typeof(StringValueNode), "2001:db8::7", true)]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4", true)]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210", true)]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A", true)]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6:7", true)]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6", true)]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5:6", true)]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5", true)]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5", true)]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4:5", true)]
    [InlineData(typeof(StringValueNode), "::1:2:3:4", true)]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4", true)]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4", true)]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3:4", true)]
    [InlineData(typeof(StringValueNode), "::1:2:3", true)]
    [InlineData(typeof(StringValueNode), "1::1:2:3", true)]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3", true)]
    [InlineData(typeof(StringValueNode), "2:1::", true)]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3", true)]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2:3", true)]
    [InlineData(typeof(StringValueNode), "::1:2", true)]
    [InlineData(typeof(StringValueNode), "1::1:2", true)]
    [InlineData(typeof(StringValueNode), "2:1::1:2", true)]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2", true)]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2", true)]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1:2", true)]
    [InlineData(typeof(StringValueNode), "::1", true)]
    [InlineData(typeof(StringValueNode), "1::1", true)]
    [InlineData(typeof(StringValueNode), "2:1::1", true)]
    [InlineData(typeof(StringValueNode), "3:2:1::1", true)]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1", true)]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1", true)]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::1", true)]
    [InlineData(typeof(StringValueNode), "::", true)]
    [InlineData(typeof(StringValueNode), "1::", true)]
    [InlineData(typeof(StringValueNode), "3:2:1::", true)]
    [InlineData(typeof(StringValueNode), "4:3:2:1::", true)]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::", true)]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::", true)]
    [InlineData(typeof(StringValueNode), "7:6:5:4:3:2:1::", true)]
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
        ExpectIsInstanceOfTypeToMatch<IPv6Type>(valueNode, expected);
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
    [InlineData("2001:db8::7/32", true)]
    [InlineData("a:b:c:d:e::1.2.3.4/13", true)]
    [InlineData("a:b:c:d:e::1.2.3.4/64", true)]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0", true)]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32", true)]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128", true)]
    [InlineData("1080:0:0:0:8:800:200C:417A/27", true)]
    [InlineData("2001:db8::7", true)]
    [InlineData("a:b:c:d:e::1.2.3.4", true)]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210", true)]
    [InlineData("1080:0:0:0:8:800:200C:417A", true)]
    [InlineData("::1:2:3:4:5:6:7", true)]
    [InlineData("::1:2:3:4:5:6", true)]
    [InlineData("1::1:2:3:4:5:6", true)]
    [InlineData("::1:2:3:4:5", true)]
    [InlineData("1::1:2:3:4:5", true)]
    [InlineData("2:1::1:2:3:4:5", true)]
    [InlineData("::1:2:3:4", true)]
    [InlineData("1::1:2:3:4", true)]
    [InlineData("2:1::1:2:3:4", true)]
    [InlineData("3:2:1::1:2:3:4", true)]
    [InlineData("::1:2:3", true)]
    [InlineData("1::1:2:3", true)]
    [InlineData("2:1::1:2:3", true)]
    [InlineData("2:1::", true)]
    [InlineData("3:2:1::1:2:3", true)]
    [InlineData("4:3:2:1::1:2:3", true)]
    [InlineData("::1:2", true)]
    [InlineData("1::1:2", true)]
    [InlineData("2:1::1:2", true)]
    [InlineData("3:2:1::1:2", true)]
    [InlineData("4:3:2:1::1:2", true)]
    [InlineData("5:4:3:2:1::1:2", true)]
    [InlineData("::1", true)]
    [InlineData("1::1", true)]
    [InlineData("2:1::1", true)]
    [InlineData("3:2:1::1", true)]
    [InlineData("4:3:2:1::1", true)]
    [InlineData("5:4:3:2:1::1", true)]
    [InlineData("6:5:4:3:2:1::1", true)]
    [InlineData("::", true)]
    [InlineData("1::", true)]
    [InlineData("3:2:1::", true)]
    [InlineData("4:3:2:1::", true)]
    [InlineData("5:4:3:2:1::", true)]
    [InlineData("6:5:4:3:2:1::", true)]
    [InlineData("7:6:5:4:3:2:1::", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<IPv6Type>(value, expected);
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
        ExpectParseLiteralToMatch<IPv6Type>(valueNode, expected);
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
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<IPv6Type>(valueNode);
    }

    [Theory]
    [InlineData("2001:db8::7/32", "2001:db8::7/32")]
    [InlineData("a:b:c:d:e::1.2.3.4/13", "a:b:c:d:e::1.2.3.4/13")]
    [InlineData("a:b:c:d:e::1.2.3.4/64", "a:b:c:d:e::1.2.3.4/64")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData("1080:0:0:0:8:800:200C:417A/27", "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData("2001:db8::7", "2001:db8::7")]
    [InlineData("a:b:c:d:e::1.2.3.4", "a:b:c:d:e::1.2.3.4")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210")]
    [InlineData("1080:0:0:0:8:800:200C:417A", "1080:0:0:0:8:800:200C:417A")]
    [InlineData("::1:2:3:4:5:6:7", "::1:2:3:4:5:6:7")]
    [InlineData("::1:2:3:4:5:6", "::1:2:3:4:5:6")]
    [InlineData("1::1:2:3:4:5:6", "1::1:2:3:4:5:6")]
    [InlineData("::1:2:3:4:5", "::1:2:3:4:5")]
    [InlineData("1::1:2:3:4:5", "1::1:2:3:4:5")]
    [InlineData("2:1::1:2:3:4:5", "2:1::1:2:3:4:5")]
    [InlineData("::1:2:3:4", "::1:2:3:4")]
    [InlineData("1::1:2:3:4", "1::1:2:3:4")]
    [InlineData("2:1::1:2:3:4", "2:1::1:2:3:4")]
    [InlineData("3:2:1::1:2:3:4", "3:2:1::1:2:3:4")]
    [InlineData("::1:2:3", "::1:2:3")]
    [InlineData("1::1:2:3", "1::1:2:3")]
    [InlineData("2:1::1:2:3", "2:1::1:2:3")]
    [InlineData("2:1::", "2:1::")]
    [InlineData("3:2:1::1:2:3", "3:2:1::1:2:3")]
    [InlineData("4:3:2:1::1:2:3", "4:3:2:1::1:2:3")]
    [InlineData("::1:2", "::1:2")]
    [InlineData("1::1:2", "1::1:2")]
    [InlineData("2:1::1:2", "2:1::1:2")]
    [InlineData("3:2:1::1:2", "3:2:1::1:2")]
    [InlineData("4:3:2:1::1:2", "4:3:2:1::1:2")]
    [InlineData("5:4:3:2:1::1:2", "5:4:3:2:1::1:2")]
    [InlineData("::1", "::1")]
    [InlineData("1::1", "1::1")]
    [InlineData("2:1::1", "2:1::1")]
    [InlineData("3:2:1::1", "3:2:1::1")]
    [InlineData("4:3:2:1::1", "4:3:2:1::1")]
    [InlineData("5:4:3:2:1::1", "5:4:3:2:1::1")]
    [InlineData("6:5:4:3:2:1::1", "6:5:4:3:2:1::1")]
    [InlineData("::", "::")]
    [InlineData("1::", "1::")]
    [InlineData("3:2:1::", "3:2:1::")]
    [InlineData("4:3:2:1::", "4:3:2:1::")]
    [InlineData("5:4:3:2:1::", "5:4:3:2:1::")]
    [InlineData("6:5:4:3:2:1::", "6:5:4:3:2:1::")]
    [InlineData("7:6:5:4:3:2:1::", "7:6:5:4:3:2:1::")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<IPv6Type>(resultValue, runtimeValue);
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
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<IPv6Type>(value);
    }

    [Theory]
    [InlineData("2001:db8::7/32", "2001:db8::7/32")]
    [InlineData("a:b:c:d:e::1.2.3.4/13", "a:b:c:d:e::1.2.3.4/13")]
    [InlineData("a:b:c:d:e::1.2.3.4/64", "a:b:c:d:e::1.2.3.4/64")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData("1080:0:0:0:8:800:200C:417A/27", "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData("2001:db8::7", "2001:db8::7")]
    [InlineData("a:b:c:d:e::1.2.3.4", "a:b:c:d:e::1.2.3.4")]
    [InlineData("FEDC:BA98:7654:3210:FEDC:BA98:7654:3210",
        "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210")]
    [InlineData("1080:0:0:0:8:800:200C:417A", "1080:0:0:0:8:800:200C:417A")]
    [InlineData("::1:2:3:4:5:6:7", "::1:2:3:4:5:6:7")]
    [InlineData("::1:2:3:4:5:6", "::1:2:3:4:5:6")]
    [InlineData("1::1:2:3:4:5:6", "1::1:2:3:4:5:6")]
    [InlineData("::1:2:3:4:5", "::1:2:3:4:5")]
    [InlineData("1::1:2:3:4:5", "1::1:2:3:4:5")]
    [InlineData("2:1::1:2:3:4:5", "2:1::1:2:3:4:5")]
    [InlineData("::1:2:3:4", "::1:2:3:4")]
    [InlineData("1::1:2:3:4", "1::1:2:3:4")]
    [InlineData("2:1::1:2:3:4", "2:1::1:2:3:4")]
    [InlineData("3:2:1::1:2:3:4", "3:2:1::1:2:3:4")]
    [InlineData("::1:2:3", "::1:2:3")]
    [InlineData("1::1:2:3", "1::1:2:3")]
    [InlineData("2:1::1:2:3", "2:1::1:2:3")]
    [InlineData("2:1::", "2:1::")]
    [InlineData("3:2:1::1:2:3", "3:2:1::1:2:3")]
    [InlineData("4:3:2:1::1:2:3", "4:3:2:1::1:2:3")]
    [InlineData("::1:2", "::1:2")]
    [InlineData("1::1:2", "1::1:2")]
    [InlineData("2:1::1:2", "2:1::1:2")]
    [InlineData("3:2:1::1:2", "3:2:1::1:2")]
    [InlineData("4:3:2:1::1:2", "4:3:2:1::1:2")]
    [InlineData("5:4:3:2:1::1:2", "5:4:3:2:1::1:2")]
    [InlineData("::1", "::1")]
    [InlineData("1::1", "1::1")]
    [InlineData("2:1::1", "2:1::1")]
    [InlineData("3:2:1::1", "3:2:1::1")]
    [InlineData("4:3:2:1::1", "4:3:2:1::1")]
    [InlineData("5:4:3:2:1::1", "5:4:3:2:1::1")]
    [InlineData("6:5:4:3:2:1::1", "6:5:4:3:2:1::1")]
    [InlineData("::", "::")]
    [InlineData("1::", "1::")]
    [InlineData("3:2:1::", "3:2:1::")]
    [InlineData("4:3:2:1::", "4:3:2:1::")]
    [InlineData("5:4:3:2:1::", "5:4:3:2:1::")]
    [InlineData("6:5:4:3:2:1::", "6:5:4:3:2:1::")]
    [InlineData("7:6:5:4:3:2:1::", "7:6:5:4:3:2:1::")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<IPv6Type>(runtimeValue, resultValue);
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
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<IPv6Type>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/13")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/64")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData(typeof(StringValueNode), "2001:db8::7")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210")]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6:7")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "::1:2:3")]
    [InlineData(typeof(StringValueNode), "1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "::1:2")]
    [InlineData(typeof(StringValueNode), "1::1:2")]
    [InlineData(typeof(StringValueNode), "2:1::1:2")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "::1")]
    [InlineData(typeof(StringValueNode), "1::1")]
    [InlineData(typeof(StringValueNode), "2:1::1")]
    [InlineData(typeof(StringValueNode), "3:2:1::1")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "::")]
    [InlineData(typeof(StringValueNode), "1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "7:6:5:4:3:2:1::")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<IPv6Type>(value, type);
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
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<IPv6Type>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/13")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4/64")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/0")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/32")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210/128")]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A/27")]
    [InlineData(typeof(StringValueNode), "2001:db8::7")]
    [InlineData(typeof(StringValueNode), "a:b:c:d:e::1.2.3.4")]
    [InlineData(typeof(StringValueNode), "FEDC:BA98:7654:3210:FEDC:BA98:7654:3210")]
    [InlineData(typeof(StringValueNode), "1080:0:0:0:8:800:200C:417A")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6:7")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5:6")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4:5")]
    [InlineData(typeof(StringValueNode), "::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3:4")]
    [InlineData(typeof(StringValueNode), "::1:2:3")]
    [InlineData(typeof(StringValueNode), "1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "2:1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2:3")]
    [InlineData(typeof(StringValueNode), "::1:2")]
    [InlineData(typeof(StringValueNode), "1::1:2")]
    [InlineData(typeof(StringValueNode), "2:1::1:2")]
    [InlineData(typeof(StringValueNode), "3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1:2")]
    [InlineData(typeof(StringValueNode), "::1")]
    [InlineData(typeof(StringValueNode), "1::1")]
    [InlineData(typeof(StringValueNode), "2:1::1")]
    [InlineData(typeof(StringValueNode), "3:2:1::1")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::1")]
    [InlineData(typeof(StringValueNode), "::")]
    [InlineData(typeof(StringValueNode), "1::")]
    [InlineData(typeof(StringValueNode), "3:2:1::")]
    [InlineData(typeof(StringValueNode), "4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "6:5:4:3:2:1::")]
    [InlineData(typeof(StringValueNode), "7:6:5:4:3:2:1::")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<IPv6Type>(value, type);
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
    [InlineData("19.117.63.126")]
    [InlineData("300.256.256.256")]
    [InlineData("255.300.256.256")]
    [InlineData("255.256.300.256")]
    [InlineData("255.256.256.300")]
    [InlineData("255.255.255.255/33")]
    [InlineData("a:b:c:d:e::1.2.3.22:100")]
    [InlineData("a:b:c:d:e::1.2.3..4/13")]
    [InlineData("FEDC:BA98:7654:3210FEDC:BA98:7654:3210")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<IPv6Type>(value);
    }
}
