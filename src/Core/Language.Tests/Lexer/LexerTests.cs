using System;
using Xunit;

namespace HotChocolate.Language
{
    public class LexerTests
    {
        [Fact]
        public void EnsureTokensAreDoublyLinked()
        {
            // arrange
            Source source = new Source(@"type foo");
            Lexer lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.Equal(TokenKind.StartOfFile, token.Kind);
            Assert.Null(token.Previous);
            Assert.NotNull(token.Next);

            Assert.Equal(TokenKind.Name, token.Next.Kind);
            Assert.Equal(token, token.Next.Previous);
            Assert.NotNull(token.Next.Next);

            Assert.Equal(TokenKind.Name, token.Next.Next.Kind);
            Assert.Equal(token.Next, token.Next.Next.Previous);
            Assert.NotNull(token.Next.Next.Next);

            Assert.Equal(TokenKind.EndOfFile, token.Next.Next.Next.Kind);
            Assert.Equal(token.Next.Next, token.Next.Next.Next.Previous);
            Assert.Null(token.Next.Next.Next.Next);
        }
    }
}