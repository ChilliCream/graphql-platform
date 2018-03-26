namespace Prometheus.Language
{
	public class NumberReader
		: TokenReaderBase
	{
		public NumberReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(c => c.IsDigit() || c.IsMinus());
		}

		public override Token ReadToken(ILexerContext context, Token previous)
		{
			int start = context.Position;
			char code = context.Read();
			bool isFloat = false;

			if (code.IsMinus())
			{
				code = context.Read();
			}

			if (code == '0')
			{
				code = context.Read();
				if (char.IsDigit(code))
				{
					throw new SyntaxException(context,
						$"Invalid number, unexpected digit after 0: {code}.");
				}
			}
			else
			{
				ReadDigits(context, code);
			}

			if (context.PeekTest(c => c.IsDot()))
			{
				isFloat = true;
				ReadDigits(context, context.Skip().Read());
			}

			if (context.PeekTest(c => c == 'E' || c == 'e'))
			{
				isFloat = true;

				code = context.Skip().Read();
				if (code.IsPlus() || code.IsMinus())
				{
					code = context.Read();
				}
				ReadDigits(context, code);
			}

			TokenKind kind = isFloat ? TokenKind.Float : TokenKind.Integer;
			return CreateToken(context, previous, kind, start,
				context.Read(start, context.Position));
		}

		private void ReadDigits(ILexerContext context, char firstCode)
		{
			if (!firstCode.IsDigit())
			{
				throw new SyntaxException(context,
					$"Invalid number, expected digit but got: {firstCode}.");
			}

			char code = firstCode;
			while (context.PeekTest(c => c.IsDigit()))
			{
				code = context.Read();
			}
		}
	}
}