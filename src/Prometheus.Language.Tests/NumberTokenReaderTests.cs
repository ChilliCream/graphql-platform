using Xunit;

namespace Prometheus.Language
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
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			NumberTokenReader reader = new NumberTokenReader(
				(a, b) => null);

			// act
			Token token = reader.ReadToken(context, previous);

			// assert
			Assert.NotNull(token);
			Assert.Equal(isFloat ? TokenKind.Float : TokenKind.Integer, token.Kind);
			Assert.Equal(sourceBody, token.Value);
			Assert.Equal(1, token.Line);
			Assert.Equal(1, token.Column);
			Assert.Equal(0, token.Start);
			Assert.Equal(sourceBody.Length, token.End);
			Assert.Equal(TokenKind.StartOfFile, token.Previous.Kind);
		}

		[InlineData("1234.123", true)]
        [InlineData("-1234.123", true)]
		[InlineData("1234", true)]
		[InlineData("-1234", true)]
		[InlineData("helloWorld_123", false)]
		[Theory]
		private void CanHandle(string sourceBody, bool expectedResult)
		{
			// arrange
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			NumberTokenReader reader = new NumberTokenReader(
				(a, b) => null);

			// act
			bool result = reader.CanHandle(context);

			// assert
			Assert.Equal(expectedResult, result);
		}
	}
}
