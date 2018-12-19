using Xunit;

namespace HotChocolate.Language
{
    public class StringTokenReaderTests
    {
        [InlineData("     \n\"üähelloWorld_123\"")]
        [InlineData("\"üähelloWorld_123\"\n        ")]
        [InlineData("     \n\"üähelloWorld_123\"\n        ")]
        [InlineData("\"üähelloWorld_123\"")]
        [Theory]
        private void ReadToken(string sourceText)
        {
            // arrange
            Source source = new Source(sourceText);

            // act
            SyntaxToken token = Lexer.Default.Read(source);
            token = token.Next;

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.String, token.Kind);
            Assert.Equal("üähelloWorld_123", token.Value);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
            Assert.Equal(TokenKind.EndOfFile, token.Next.Kind);
        }

        // allowed escape characters "	\	/	b	f	n	r	t
        // see also http://facebook.github.io/graphql/draft/#EscapedCharacter
        // \" -> "
        [InlineData("\"\\\"456\"", "\"456")]
        [InlineData("\"123\\\"456\"", "123\"456")]
        [InlineData("\"123\\\"\"", "123\"")]
        [InlineData("\"\\\"\"", "\"")]

        // \\ -> \
        [InlineData("\"\\\\456\"", "\\456")]
        [InlineData("\"123\\\\456\"", "123\\456")]
        [InlineData("\"123\\\\\"", "123\\")]
        [InlineData("\"\\\\\"", "\\")]

        // \/ -> /
        [InlineData("\"\\/456\"", "/456")]
        [InlineData("\"123\\/456\"", "123/456")]
        [InlineData("\"123\\/\"", "123/")]
        [InlineData("\"\\/\"", "/")]

        [InlineData("\"123\\b456\"", "123\b456")]
        [InlineData("\"123\\f456\"", "123\f456")]
        [InlineData("\"123\\n456\"", "123\n456")]
        [InlineData("\"123\\r456\"", "123\r456")]
        [InlineData("\"123\\t456\"", "123\t456")]

        [Theory]
        private void EscapeCharacters(string sourceText, string expectedResult)
        {
            // arrange
            Source source = new Source(sourceText);

            // act
            SyntaxToken token = Lexer.Default.Read(source);
            token = token.Next;

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.String, token.Kind);
            Assert.Equal(expectedResult, token.Value);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
            Assert.Equal(TokenKind.EndOfFile, token.Next.Kind);
        }
    }
}
