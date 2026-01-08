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
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "#000", true)]
    [InlineData(typeof(StringValueNode), "#FFFFFF", true)]
    [InlineData(typeof(StringValueNode), "#A52A2A", true)]
    [InlineData(typeof(StringValueNode), "#800080", true)]
    [InlineData(typeof(StringValueNode), "#09C", true)]
    [InlineData(typeof(StringValueNode), "#0099CC", true)]
    [InlineData(typeof(StringValueNode), "#FFA500", true)]
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
        ExpectIsInstanceOfTypeToMatch<HexColorType>(valueNode, expected);
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
        ExpectParseLiteralToMatch<HexColorType>(valueNode, expected);
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
    public void CoerceInputLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<HexColorType>(valueNode);
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
    public void CoerceInputValue_GivenValue_ThrowSerializationException(string jsonValue)
    {
        // arrange
        // act
        // assert
        ExpectCoerceInputValueToThrowSerializationException<HexColorType>(jsonValue);
    }

    [Theory]
    [InlineData("#000")]
    [InlineData("#FFFFFF")]
    [InlineData("#A52A2A")]
    [InlineData("#800080")]
    [InlineData("#09C")]
    [InlineData("#0099CC")]
    [InlineData("#FFA500")]
    public void CoerceOutputValue_GivenObject_MatchExpectedType(object? runtimeValue)
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
    public void CoerceOutputValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrowSerializationException<HexColorType>(value);
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
    [InlineData(typeof(NullValueNode), null)]
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
