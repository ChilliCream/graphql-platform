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
