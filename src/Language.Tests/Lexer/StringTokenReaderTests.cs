using Xunit;

namespace HotChocolate.Language
{
    /*
    public class StringTokenReaderTests
    {
        [Fact]
        private void ReadToken()
        {
            // arrange
            string sourceBody = "\"üähelloWorld_123\"";
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            StringTokenReader reader = new StringTokenReader();

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.String, token.Kind);
            Assert.Equal("üähelloWorld_123", token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

        [InlineData("\"helloWorld_123\"", true)]
        [InlineData("\"\"helloWorld_123\"\"\"", true)]
        [InlineData("\"\"\"helloWorld_123\"\"\"", false)]
        [Theory]
        private void CanHandle(string sourceBody, bool expectedResult)
        {
            // arrange
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            StringTokenReader reader = new StringTokenReader();

            // act
            bool result = reader.CanHandle(context);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
     */
}