using HotChocolate.Language;

namespace HotChocolate.Types;

public class MacAddressTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<MacAddressType>();

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
    [InlineData(typeof(StringValueNode), "f", false)]
    [InlineData(typeof(StringValueNode), "ff", false)]
    [InlineData(typeof(StringValueNode), "ff:ff:ff", false)]
    [InlineData(typeof(StringValueNode), "ff:ff:ff:ff:ff", false)]
    [InlineData(typeof(StringValueNode), "ff:ff:ff:ff:ff:ff", true)]
    [InlineData(typeof(StringValueNode), "ff-ff-ff-ff-ff-ff", true)]
    [InlineData(typeof(StringValueNode), "ff:ff:ff:ff:ff:ff:", false)]
    [InlineData(typeof(StringValueNode), "gf:ff:ff:ff:ff:ff", false)]
    [InlineData(typeof(StringValueNode), "fff:ff:ff:ff:ff:ff", false)]
    [InlineData(typeof(StringValueNode), "11:11:11:11:11:11", true)]
    [InlineData(typeof(StringValueNode), "00:00:00:00:00:00", true)]
    [InlineData(typeof(StringValueNode), "a0:93:db:60:3b:72", true)]
    [InlineData(typeof(StringValueNode), "9d:f7:56:d1:73:a4", true)]
    [InlineData(typeof(StringValueNode), "9d-f7-56-d1-73-a4", true)]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:9f:13:78", true)]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-9f-13-78", true)]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-ff-fe-9f-13-78", true)]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:ff:fe:9f:13:78", true)]
    [InlineData(typeof(StringValueNode), "fa7e.9eff.fe9f.1378", true)]
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
        ExpectIsInstanceOfTypeToMatch<MacAddressType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("f", false)]
    [InlineData("ff", false)]
    [InlineData("ff:ff:ff", false)]
    [InlineData("ff:ff:ff:ff:ff", false)]
    [InlineData("ff:ff:ff:ff:ff:ff", true)]
    [InlineData("ff-ff-ff-ff-ff-ff", true)]
    [InlineData("ff:ff:ff:ff:ff:ff:", false)]
    [InlineData("gf:ff:ff:ff:ff:ff", false)]
    [InlineData("fff:ff:ff:ff:ff:ff", false)]
    [InlineData("11:11:11:11:11:11", true)]
    [InlineData("00:00:00:00:00:00", true)]
    [InlineData("a0:93:db:60:3b:72", true)]
    [InlineData("9d:f7:56:d1:73:a4", true)]
    [InlineData("9d-f7-56-d1-73-a4", true)]
    [InlineData("fa:7e:9e:9f:13:78", true)]
    [InlineData("fa-7e-9e-9f-13-78", true)]
    [InlineData("fa-7e-9e-ff-fe-9f-13-78", true)]
    [InlineData("fa:7e:9e:ff:fe:9f:13:78", true)]
    [InlineData("fa7e.9eff.fe9f.1378", true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<MacAddressType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-9f-13-78", "fa-7e-9e-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:9f:13:78", "fa:7e:9e:9f:13:78")]
    [InlineData(typeof(StringValueNode), "9d-f7-56-d1-73-a4", "9d-f7-56-d1-73-a4")]
    [InlineData(typeof(StringValueNode), "00:00:00:00:00:00", "00:00:00:00:00:00")]
    [InlineData(typeof(StringValueNode), "ff-ff-ff-ff-ff-ff", "ff-ff-ff-ff-ff-ff")]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-ff-fe-9f-13-78", "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:ff:fe:9f:13:78", "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData(typeof(StringValueNode), "fa7e.9eff.fe9f.1378", "fa7e.9eff.fe9f.1378")]
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
        ExpectParseLiteralToMatch<MacAddressType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(IntValueNode), 12345)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "f")]
    [InlineData(typeof(StringValueNode), "ff")]
    [InlineData(typeof(StringValueNode), "ff:ff:ff")]
    [InlineData(typeof(StringValueNode), "ff:ff:ff:ff:ff")]
    [InlineData(typeof(StringValueNode), "ff:ff:ff:ff:ff:ff:")]
    [InlineData(typeof(StringValueNode), "gf:ff:ff:ff:ff:ff")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<MacAddressType>(valueNode);
    }

    [Theory]
    [InlineData("fa-7e-9e-9f-13-78", "fa-7e-9e-9f-13-78")]
    [InlineData("fa:7e:9e:9f:13:78", "fa:7e:9e:9f:13:78")]
    [InlineData("9d-f7-56-d1-73-a4", "9d-f7-56-d1-73-a4")]
    [InlineData("00:00:00:00:00:00", "00:00:00:00:00:00")]
    [InlineData("ff-ff-ff-ff-ff-ff", "ff-ff-ff-ff-ff-ff")]
    [InlineData("fa-7e-9e-ff-fe-9f-13-78", "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData("fa:7e:9e:ff:fe:9f:13:78", "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData("fa7e.9eff.fe9f.1378", "fa7e.9eff.fe9f.1378")]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object? resultValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<MacAddressType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("ff")]
    [InlineData("ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff:ff:")]
    [InlineData("gf:ff:ff:ff:ff:ff")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<EmailAddressType>(value);
    }

    [Theory]
    [InlineData("fa-7e-9e-9f-13-78", "fa-7e-9e-9f-13-78")]
    [InlineData("fa:7e:9e:9f:13:78", "fa:7e:9e:9f:13:78")]
    [InlineData("9d-f7-56-d1-73-a4", "9d-f7-56-d1-73-a4")]
    [InlineData("00:00:00:00:00:00", "00:00:00:00:00:00")]
    [InlineData("ff-ff-ff-ff-ff-ff", "ff-ff-ff-ff-ff-ff")]
    [InlineData("fa-7e-9e-ff-fe-9f-13-78", "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData("fa:7e:9e:ff:fe:9f:13:78", "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData("fa7e.9eff.fe9f.1378", "fa7e.9eff.fe9f.1378")]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object? runtimeValue,
        object? resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<MacAddressType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("ff")]
    [InlineData("ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff:ff:")]
    [InlineData("gf:ff:ff:ff:ff:ff")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<MacAddressType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:9f:13:78")]
    [InlineData(typeof(StringValueNode), "9d-f7-56-d1-73-a4")]
    [InlineData(typeof(StringValueNode), "00:00:00:00:00:00")]
    [InlineData(typeof(StringValueNode), "ff-ff-ff-ff-ff-ff")]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData(typeof(StringValueNode), "fa7e.9eff.fe9f.1378")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<MacAddressType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("ff")]
    [InlineData("ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff:ff:")]
    [InlineData("gf:ff:ff:ff:ff:ff")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<MacAddressType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:9f:13:78")]
    [InlineData(typeof(StringValueNode), "9d-f7-56-d1-73-a4")]
    [InlineData(typeof(StringValueNode), "00:00:00:00:00:00")]
    [InlineData(typeof(StringValueNode), "ff-ff-ff-ff-ff-ff")]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData(typeof(StringValueNode), "fa7e.9eff.fe9f.1378")]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<MacAddressType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("ff")]
    [InlineData("ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff")]
    [InlineData("ff:ff:ff:ff:ff:ff:")]
    [InlineData("gf:ff:ff:ff:ff:ff")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<MacAddressType>(value);
    }
}
