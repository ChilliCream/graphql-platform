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
    [InlineData(typeof(StringValueNode), "fa-7e-9e-9f-13-78", "fa-7e-9e-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:9f:13:78", "fa:7e:9e:9f:13:78")]
    [InlineData(typeof(StringValueNode), "9d-f7-56-d1-73-a4", "9d-f7-56-d1-73-a4")]
    [InlineData(typeof(StringValueNode), "00:00:00:00:00:00", "00:00:00:00:00:00")]
    [InlineData(typeof(StringValueNode), "ff-ff-ff-ff-ff-ff", "ff-ff-ff-ff-ff-ff")]
    [InlineData(typeof(StringValueNode), "fa-7e-9e-ff-fe-9f-13-78", "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData(typeof(StringValueNode), "fa:7e:9e:ff:fe:9f:13:78", "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData(typeof(StringValueNode), "fa7e.9eff.fe9f.1378", "fa7e.9eff.fe9f.1378")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<MacAddressType>(valueNode, expected);
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
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<MacAddressType>(valueNode);
    }

    [Theory]
    [InlineData("\"fa-7e-9e-9f-13-78\"", "fa-7e-9e-9f-13-78")]
    [InlineData("\"fa:7e:9e:9f:13:78\"", "fa:7e:9e:9f:13:78")]
    [InlineData("\"9d-f7-56-d1-73-a4\"", "9d-f7-56-d1-73-a4")]
    [InlineData("\"00:00:00:00:00:00\"", "00:00:00:00:00:00")]
    [InlineData("\"ff-ff-ff-ff-ff-ff\"", "ff-ff-ff-ff-ff-ff")]
    [InlineData("\"fa-7e-9e-ff-fe-9f-13-78\"", "fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData("\"fa:7e:9e:ff:fe:9f:13:78\"", "fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData("\"fa7e.9eff.fe9f.1378\"", "fa7e.9eff.fe9f.1378")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<MacAddressType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("\"\"")]
    [InlineData("\"f\"")]
    [InlineData("\"ff\"")]
    [InlineData("\"ff:ff:ff\"")]
    [InlineData("\"ff:ff:ff:ff:ff\"")]
    [InlineData("\"ff:ff:ff:ff:ff:ff:\"")]
    [InlineData("\"gf:ff:ff:ff:ff:ff\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<MacAddressType>(jsonValue);
    }

    [Theory]
    [InlineData("fa-7e-9e-9f-13-78")]
    [InlineData("fa:7e:9e:9f:13:78")]
    [InlineData("9d-f7-56-d1-73-a4")]
    [InlineData("00:00:00:00:00:00")]
    [InlineData("ff-ff-ff-ff-ff-ff")]
    [InlineData("fa-7e-9e-ff-fe-9f-13-78")]
    [InlineData("fa:7e:9e:ff:fe:9f:13:78")]
    [InlineData("fa7e.9eff.fe9f.1378")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<MacAddressType>(runtimeValue);
    }

    [Theory]
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
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<MacAddressType>(value);
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
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<MacAddressType>(value, type);
    }

    [Theory]
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
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<MacAddressType>(value);
    }
}
