using System;
using Xunit;

namespace HotChocolate.Language
{
    /*
    public class PunctuatorTokenReaderTests
    {
        [Fact]
        public void ReadBangToken()
        {
            ReadToken('!', TokenKind.Bang);
        }

        [Fact]
        public void ReadDollarToken()
        {
            ReadToken('$', TokenKind.Dollar);
        }

        [Fact]
        public void ReadAmpersandToken()
        {
            ReadToken('&', TokenKind.Ampersand);
        }

        [Fact]
        public void ReadLeftParenthesisToken()
        {
            ReadToken('(', TokenKind.LeftParenthesis);
        }

        [Fact]
        public void ReadRightParenthesisToken()
        {
            ReadToken(')', TokenKind.RightParenthesis);
        }

        [Fact]
        public void ReadColonToken()
        {
            ReadToken(':', TokenKind.Colon);
        }

        [Fact]
        public void ReadEqualToken()
        {
            ReadToken('=', TokenKind.Equal);
        }

        [Fact]
        public void ReadAtToken()
        {
            ReadToken('@', TokenKind.At);
        }

        [Fact]
        public void ReadLeftBracketToken()
        {
            ReadToken('[', TokenKind.LeftBracket);
        }
        [Fact]
        public void ReadRightBracketToken()
        {
            ReadToken(']', TokenKind.RightBracket);
        }
        [Fact]
        public void ReadLeftBraceToken()
        {
            ReadToken('{', TokenKind.LeftBrace);
        }
        [Fact]
        public void ReadRightBraceToken()
        {
            ReadToken('}', TokenKind.RightBrace);
        }

        [Fact]
        public void ReadPipeToken()
        {
            ReadToken('|', TokenKind.Pipe);
        }

        [Fact]
        public void ReadSpreadToken()
        {
            ReadToken("...", TokenKind.Spread);
        }

        private void ReadToken(char code, TokenKind kind)
        {
            ReadToken(code.ToString(), kind);
        }

        private void ReadToken(string sourceBody, TokenKind kind)
        {
            // arrange
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            PunctuatorTokenReader reader = new PunctuatorTokenReader();

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
            Assert.Equal(kind, token.Kind);
            Assert.Null(token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

        [Fact]
        public void ReadInvalidToken()
        {
            // arrange
            Source source = new Source("z");
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            PunctuatorTokenReader reader = new PunctuatorTokenReader();

            // act
            Action read = () => reader.ReadToken(context, previous);

            // assert
            Assert.Throws<SyntaxException>(read);
        }

        [InlineData("x", false)]
        [InlineData(".", true)]
        [InlineData("!", true)]
        [InlineData("$", true)]
        [InlineData("&", true)]
        [InlineData("(", true)]
        [InlineData(")", true)]
        [InlineData(":", true)]
        [InlineData("=", true)]
        [InlineData("@", true)]
        [InlineData("[", true)]
        [InlineData("]", true)]
        [InlineData("{", true)]
        [InlineData("}", true)]
        [InlineData("|", true)]
        [Theory]
        public void CanHandle(string token, bool expectedResult)
        {
            // arrange
            Source source = new Source(token);
            LexerContext context = new LexerContext(source);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1, null);
            PunctuatorTokenReader reader = new PunctuatorTokenReader();

            // act
            bool result = reader.CanHandle(context);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }

     */
}