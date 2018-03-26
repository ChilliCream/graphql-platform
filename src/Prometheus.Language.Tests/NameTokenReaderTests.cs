using System;
using Xunit;

namespace Prometheus.Language
{
	public class NameTokenReaderTests
	{
		[Fact]
		private void ReadToken()
		{
			// arrange
			string sourceBody = "helloWorld_123";
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			NameTokenReader reader = new NameTokenReader(
				(a, b) => null);

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

		[InlineData("123_helloWorld", false)]
		[InlineData("helloWorld_123", true)]
		[Theory]
		private void CanHandle(string sourceBody, bool expectedResult)
		{
			// arrange
			Source source = new Source(sourceBody);
			LexerContext context = new LexerContext(source);

			//CreateToken(context, null, TokenKind.StartOfFile, 0);
			Token previous = new Token(TokenKind.StartOfFile, 0, 0, 1, 1,
				null, new Thunk<Token>((Token)null));

			NameTokenReader reader = new NameTokenReader(
				(a, b) => null);

			// act
			bool result = reader.CanHandle(context);

			// assert
			Assert.Equal(expectedResult, result);
		}
	}
}
