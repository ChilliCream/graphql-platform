using System.Text;

namespace Prometheus.Language
{
	/// <summary>
	/// Reads string tokens specified in 
	/// http://facebook.github.io/graphql/October2016/#StringValue
	/// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
	/// </summary>
	public class StringTokenReader
		: TokenReaderBase
	{
		public StringTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(c => c.IsQuote());
		}

		public override Token ReadToken(ILexerContext context, Token previous)
		{
			int chunkStart = context.Position;
			StringBuilder value = new StringBuilder();

			while (context.PeekTest(c => c != 0x000a && c != 0x000d))
			{
				// Closing Quote (")
				char code = context.Read();
				if (code.IsQuote())
				{
					value.Append(context.Read(chunkStart, context.Position));
					return CreateToken(context, previous,
						TokenKind.String, chunkStart, value.ToString());
				}

				// SourceCharacter
				if (code < 0x0020 && code != 0x0009)
				{
					throw new SyntaxException(context,
					  $"Invalid character within String: {code}.");
				}

				if (code.IsBackslash())
				{
					value.Append(context.Read(chunkStart, context.Position));
					value.Append(ReadEscapedChar(context));
					chunkStart = context.Position;
				}
			}

			throw new SyntaxException(context, "Unterminated string.");
		}

		private char ReadEscapedChar(ILexerContext context)
		{
			char code = context.Read();

			if (code.IsValidEscapeCharacter())
			{
				return code;
			}

			if (code == 'u')
			{
				if (!TryReadUnicodeChar(context, out code))
				{
					throw new SyntaxException(context,
						"Invalid character escape sequence: " +
						$"\\u{context.Read(context.Position - 4, context.Position)}.");
				}
				return code;
			}

			throw new SyntaxException(context,
				$"Invalid character escape sequence: \\{code}.");
		}

		private bool TryReadUnicodeChar(ILexerContext context, out char code)
		{
			int c = (CharToHex(context.Read()) << 12)
				| (CharToHex(context.Read()) << 8)
				| (CharToHex(context.Read()) << 4)
				| CharToHex(context.Read());

			if (c < 0)
			{
				code = default(char);
				return false;
			}

			code = (char)c;
			return true;
		}

		public int CharToHex(int a)
		{
			return a >= 48 && a <= 57
			  ? a - 48 // 0-9
			  : a >= 65 && a <= 70
				? a - 55 // A-F
				: a >= 97 && a <= 102
				  ? a - 87 // a-f
				  : -1;
		}
	}
}
