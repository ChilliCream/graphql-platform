using Xunit;

namespace HotChocolate.Language
{
    public class NumberTokenReaderTests
    {
        [InlineData("1234.123", true)]
        [InlineData("-1234.123", true)]
        [InlineData("1234", false)]
        [InlineData("-1234", false)]
        [InlineData("1e50", true)]
        [InlineData("6.0221413e23", true)]
        [Theory]
        private void ReadToken(string sourceBody, bool isFloat)
        {
            // arrange
            Source source = new Source(sourceBody);

            // act
            SyntaxToken token = Lexer.Default.Read(source);
            token = token.Next;

            // assert
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
            Assert.Equal(TokenKind.EndOfFile, token.Next.Kind);
            Assert.NotNull(token);
            Assert.Equal(isFloat ? TokenKind.Float : TokenKind.Integer, token.Kind);
            Assert.Equal(sourceBody, token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }
    }
}
