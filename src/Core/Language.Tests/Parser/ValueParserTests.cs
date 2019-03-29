using System;
using Xunit;

namespace HotChocolate.Language
{
    public class ValueParserTests
    {
        [InlineData("true", true, typeof(BooleanValueNode))]
        [InlineData("false", false, typeof(BooleanValueNode))]
        [InlineData("0", "0", typeof(IntValueNode))]
        [InlineData("123", "123", typeof(IntValueNode))]
        [InlineData("123.456", "123.456", typeof(FloatValueNode))]
        [InlineData("\"123\"", "123", typeof(StringValueNode))]
        [InlineData("\"\\u004E\"", "N", typeof(StringValueNode))]
        [InlineData("FOO", "FOO", typeof(EnumValueNode))]
        [Theory]
        public void ParseSimpleValue(
            string value,
            object expectedValue,
            Type expectedNodeType)
        {
            // arrange
            SyntaxToken start = Lexer.Default.Read(
                new Source(value));

            var context = new ParserContext(
                new Source(value),
                start,
                ParserOptions.Default,
                Parser.ParseName);
            context.MoveNext();

            // act
            IValueNode valueNode = ParseValue(value);

            // assert
            Assert.Equal(expectedNodeType, valueNode.GetType());
            Assert.Equal(expectedValue, valueNode.Value);
        }

        private static IValueNode ParseValue(string value)
        {
            SyntaxToken start = Lexer.Default.Read(
                new Source(value));

            var context = new ParserContext(
                new Source(value),
                start,
                ParserOptions.Default,
                Parser.ParseName);
            context.MoveNext();

            return Parser.ParseValueLiteral(context, true);
        }
    }
}

