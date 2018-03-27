using Xunit;

namespace Prometheus.Language
{
	public class BlockStringTokenReaderTests
	{
		[Fact]
		private void ReadToken()
		{
			// arrange
			string sourceBody = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			BlockStringTokenReader reader = new BlockStringTokenReader(
				(a, b) => null);

			// act
			Token token = reader.ReadToken(context, previous);

			// assert
			Assert.NotNull(token);
			Assert.Equal(TokenKind.BlockString, token.Kind);
			Assert.Equal("helloWorld_123\n\tfoo\nbar", token.Value);
			Assert.Equal(1, token.Line);
			Assert.Equal(1, token.Column);
			Assert.Equal(0, token.Start);
			Assert.Equal(sourceBody.Length, token.End);
			Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
		}

		[Fact]
        private void ReadToken_WithEscapedTrippleQuote_EscapeIsReplacedWithActualQuotes()
        {
            // arrange
			string sourceBody = "\"\"\"\\\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            Source source = new Source(sourceBody);
            LexerContext context = new LexerContext(source);

            //CreateToken(context, null, TokenKind.StartOfFile, 0);
            Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
                null, new Thunk<Token>((Token)null));

            BlockStringTokenReader reader = new BlockStringTokenReader(
                (a, b) => null);

            // act
            Token token = reader.ReadToken(context, previous);

            // assert
            Assert.NotNull(token);
			Assert.Equal(TokenKind.BlockString, token.Kind);
			Assert.Equal("\"\"\"helloWorld_123\n\tfoo\nbar", token.Value);
            Assert.Equal(1, token.Line);
            Assert.Equal(1, token.Column);
            Assert.Equal(0, token.Start);
            Assert.Equal(sourceBody.Length, token.End);
            Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
        }

		[Fact]
		private void ReadToken_WithLeadingBlanks_BlanksAreRemoved()
		{
			// arrange
			string sourceBody = "\"\"\"\r\n\t\r\n\t\r\n\thelloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			BlockStringTokenReader reader = new BlockStringTokenReader(
				(a, b) => null);

			// act
			Token token = reader.ReadToken(context, previous);

			// assert
			Assert.NotNull(token);
			Assert.Equal(TokenKind.BlockString, token.Kind);
			Assert.Equal("helloWorld_123\n\tfoo\nbar", token.Value);
			Assert.Equal(1, token.Line);
			Assert.Equal(1, token.Column);
			Assert.Equal(0, token.Start);
			Assert.Equal(sourceBody.Length, token.End);
			Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
		}

		[Fact]
		private void ReadToken_WithTrailingBlanks_BlanksAreRemoved()
		{
			// arrange
			string sourceBody = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\r\n\t\r\n\t\r\n\t\r\n\t\"\"\"";
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			BlockStringTokenReader reader = new BlockStringTokenReader(
				(a, b) => null);

			// act
			Token token = reader.ReadToken(context, previous);

			// assert
			Assert.NotNull(token);
			Assert.Equal(TokenKind.BlockString, token.Kind);
			Assert.Equal("helloWorld_123\n\tfoo\nbar", token.Value);
			Assert.Equal(1, token.Line);
			Assert.Equal(1, token.Column);
			Assert.Equal(0, token.Start);
			Assert.Equal(sourceBody.Length, token.End);
			Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
		}

		[InlineData("\"helloWorld_123\"\"\"", false)]
		[InlineData("\"\"helloWorld_123\"\"\"", false)]
		[InlineData("\"\"\"helloWorld_123\"\"\"", true)]
		[Theory]
		private void CanHandle(string sourceBody, bool expectedResult)
		{
			// arrange
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			BlockStringTokenReader reader = new BlockStringTokenReader(
				(a, b) => null);

			// act
			bool result = reader.CanHandle(context);

			// assert
			Assert.Equal(expectedResult, result);
		}
	}
}
