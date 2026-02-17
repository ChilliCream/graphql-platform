using HotChocolate.Language;

namespace HotChocolate.Types;

public class HexColorTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<HexColorType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "#000", "#000")]
    [InlineData(typeof(StringValueNode), "#FFFFFF", "#FFFFFF")]
    [InlineData(typeof(StringValueNode), "#A52A2A", "#A52A2A")]
    [InlineData(typeof(StringValueNode), "#800080", "#800080")]
    [InlineData(typeof(StringValueNode), "#09C", "#09C")]
    [InlineData(typeof(StringValueNode), "#0099CC", "#0099CC")]
    [InlineData(typeof(StringValueNode), "#FFA500", "#FFA500")]
    [InlineData(typeof(StringValueNode), "#CcC", "#CcC")]
    public void CoerceInputLiteral_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        object? expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToMatch<HexColorType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(IntValueNode), 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "090")]
    [InlineData(typeof(StringValueNode), "FFFFFF")]
    [InlineData(typeof(StringValueNode), "FF A5 00")]
    [InlineData(typeof(StringValueNode), "#009CC")]
    [InlineData(typeof(StringValueNode), "#80 00 80")]
    [InlineData(typeof(StringValueNode), "#0000")]
    public void CoerceInputLiteral_GivenValueNode_Throw(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectCoerceInputLiteralToThrow<HexColorType>(valueNode);
    }

    [Theory]
    [InlineData("\"#000\"", "#000")]
    [InlineData("\"#FFFFFF\"", "#FFFFFF")]
    [InlineData("\"#A52A2A\"", "#A52A2A")]
    [InlineData("\"#800080\"", "#800080")]
    [InlineData("\"#09C\"", "#09C")]
    [InlineData("\"#0099CC\"", "#0099CC")]
    [InlineData("\"#FFA500\"", "#FFA500")]
    public void CoerceInputValue_GivenValue_MatchExpected(
        string jsonValue,
        object? runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToMatch<HexColorType>(jsonValue, runtimeValue);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("\"090\"")]
    [InlineData("\"FFFFFF\"")]
    [InlineData("\"FF A5 00\"")]
    [InlineData("\"#009CC\"")]
    [InlineData("\"80 00 80\"")]
    [InlineData("\"0000\"")]
    public void CoerceInputValue_GivenValue_Throw(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrow<HexColorType>(jsonValue);
    }

    [Theory]
    [InlineData("#000")]
    [InlineData("#FFFFFF")]
    [InlineData("#A52A2A")]
    [InlineData("#800080")]
    [InlineData("#09C")]
    [InlineData("#0099CC")]
    [InlineData("#FFA500")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<HexColorType>(runtimeValue);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("090")]
    [InlineData("FFFFFF")]
    [InlineData("FF A5 00")]
    [InlineData("#009CC")]
    [InlineData("80 00 80")]
    [InlineData("0000")]
    public void CoerceOutputValue_GivenObject_Throw(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<HexColorType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "#000")]
    [InlineData(typeof(StringValueNode), "#FFFFFF")]
    [InlineData(typeof(StringValueNode), "#A52A2A")]
    [InlineData(typeof(StringValueNode), "#800080")]
    [InlineData(typeof(StringValueNode), "#09C")]
    [InlineData(typeof(StringValueNode), "#0099CC")]
    [InlineData(typeof(StringValueNode), "#FFA500")]
    [InlineData(typeof(StringValueNode), "#CcC")]
    public void ValueToLiteral_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToMatchType<HexColorType>(value, type);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1)]
    [InlineData(true)]
    [InlineData("090")]
    [InlineData("FFFFFF")]
    [InlineData("FF A5 00")]
    [InlineData("#009CC")]
    [InlineData("80 00 80")]
    [InlineData("0000")]
    public void ValueToLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectValueToLiteralToThrowSerializationException<HexColorType>(value);
    }
}
