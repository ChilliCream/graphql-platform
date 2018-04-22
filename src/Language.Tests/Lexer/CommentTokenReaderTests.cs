using Xunit;

namespace HotChocolate.Language
{
    /*
    public class CommentTokenReaderTests
    {
        [Fact]
        private void ReadToken()
        {
            // arrange
            string sourceBody = "# my comment foo bar";
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            CommentTokenReader reader = new CommentTokenReader();

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.Comment, token.Kind);
            Assert.Equal(sourceBody.Substring(2), token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

        [InlineData("not a comment", false)]
        [InlineData("# this is a valid comment", true)]
        [Theory]
        private void CanHandle(string sourceBody, bool expectedResult)
        {
            // arrange
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            CommentTokenReader reader = new CommentTokenReader();

            // act
            bool result = reader.CanHandle(context);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
     */
}
