using Xunit;

namespace HotChocolate.Language
{
    public class CommentTokenReaderTests
    {
        [InlineData("# my comment foo bar")]
        [InlineData("# my comment foo bar\n   ")]
        [InlineData("     \n# my comment foo bar")]
        [InlineData("     \n# my comment foo bar\n    ")]
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
            Assert.Equal(TokenKind.Comment, token.Kind);
            Assert.Equal("my comment foo bar", token.Value);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
            Assert.Equal(TokenKind.EndOfFile, token.Next.Kind);
        }
    }
}
