using System;
using Xunit;

namespace HotChocolate.Language;

public class ValueParserTests
{
    [InlineData("true", true, typeof(BooleanValueNode))]
    [InlineData("false", false, typeof(BooleanValueNode))]
    [InlineData("0", "0", typeof(IntValueNode))]
    [InlineData("123", "123", typeof(IntValueNode))]
    [InlineData("-123", "-123", typeof(IntValueNode))]
    [InlineData("2.3e-5", "2.3e-5", typeof(FloatValueNode))]
    [InlineData("2.3e+5", "2.3e+5", typeof(FloatValueNode))]
    [InlineData("123.456", "123.456", typeof(FloatValueNode))]
    [InlineData("\"123\"", "123", typeof(StringValueNode))]
    [InlineData("\"\"\"123\n456\"\"\"", "123\n456", typeof(StringValueNode))]
    [InlineData("\"\\u0031\"", "1", typeof(StringValueNode))]
    [InlineData("\"\\u004E\"", "N", typeof(StringValueNode))]
    [InlineData("\"\\u0061\"", "a", typeof(StringValueNode))]
    [InlineData("\"\\u003A\"", ":", typeof(StringValueNode))]
    [InlineData("FOO", "FOO", typeof(EnumValueNode))]
    [Theory]
    public void ParseSimpleValue(
        string value,
        object expectedValue,
        Type expectedNodeType)
    {
        // arrange
        // act
        var valueNode = ParseValue(value);

        // assert
        Assert.Equal(expectedNodeType, valueNode.GetType());
        Assert.Equal(expectedValue, valueNode.Value);
    }

    [Fact]
    public void ZeroZeroIsNotAllowed()
    {
        // arrange
        // act
        static void Action() => ParseValue("00");

        // assert
        Assert.Throws<SyntaxException>(Action);
    }

    private static IValueNode ParseValue(string value)
        => Utf8GraphQLParser.Syntax.ParseValueLiteral(value, true);
}
