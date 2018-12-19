using System;
using Xunit;

namespace HotChocolate.Language
{
    public class NameTokenReaderTests
    {
        [InlineData("    \nhelloWorld_123")]
        [InlineData("    \nhelloWorld_123\n     ")]
        [InlineData("helloWorld_123\n     ")]
        [InlineData("helloWorld_123")]
        [Theory]
        private void ReadToken(string sourceText)
        {
            // arrange
            string nameTokenValue = "helloWorld_123";
            Source source = new Source(sourceText);

            // act
            SyntaxToken token = Lexer.Default.Read(source);
            token = token.Next;

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.Name, token.Kind);
            Assert.Equal(nameTokenValue, token.Value);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
            Assert.Equal(TokenKind.EndOfFile, token.Next.Kind);
        }
    }
}
