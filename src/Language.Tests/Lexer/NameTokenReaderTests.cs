using System;
using Xunit;

namespace HotChocolate.Language
{
    /*
    public class NameTokenReaderTests
    {
        [Fact]
        private void ReadToken()
        {
            // arrange
            string sourceBody = "helloWorld_123";
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            NameTokenReader reader = new NameTokenReader();

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.Name, token.Kind);
            Assert.Equal(sourceBody, token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

        [Fact]
        private void ReadToken_WithNonAsciiChar_SyntaxException()
        {
            // arrange
            string sourceBody = "hellöWorld_123";
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            NameTokenReader reader = new NameTokenReader();

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
            Assert.Equal(TokenKind.Name, token.Kind);
            Assert.Equal("hell", token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(4, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

        [InlineData("123_helloWorld", false)]
        [InlineData("ähelloWorld_123", false)]
        [InlineData("helloWorld_123", true)]
        [Theory]
        private void CanHandle(string sourceBody, bool expectedResult)
        {
            // arrange
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            NameTokenReader reader = new NameTokenReader();

            // act
            bool result = reader.CanHandle(context);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
     */
}