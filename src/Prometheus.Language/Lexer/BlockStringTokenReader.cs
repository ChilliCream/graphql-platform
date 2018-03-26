using System;
using System.Linq;
using System.Text;

namespace Prometheus.Language
{
	public class BlockStringTokenReader
		: TokenReaderBase
	{
		public BlockStringTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(
				c => c.IsQuote(),
				c => c.IsQuote(),
				c => c.IsQuote());
		}

		public override Token ReadToken(ILexerContext context, Token previous)
		{
			StringBuilder rawValue = new StringBuilder();
			int start = context.Position;
			int chunkStart = context.Position + 3;

			context.Skip(3);

			while (!context.IsEndOfStream())
			{
				char code = context.Read(); ;

				// Closing Triple-Quote (""")
				if (code.IsQuote() && context.PeekTest(c => c.IsQuote(), c => c.IsQuote()))
				{
					context.Skip(2);
					rawValue.Append(context.Read(chunkStart, context.Position - 3));
					CreateToken(context, previous, TokenKind.BlockString,
						start, TrimBlockStringValue(rawValue.ToString()));
				}

				// SourceCharacter
				if (code < 0x0020 && code != 0x0009
					&& code != 0x000a && code != 0x000d)
				{
					throw new SyntaxException(context,
						$"Invalid character within String: ${code}.");
				}

				// Escape Triple-Quote (\""")
				if (code.IsBackslash() && context.PeekTest(
					c => c.IsQuote(), c => c.IsQuote(), c => c.IsQuote()))
				{
					rawValue.Append(context.Read(chunkStart, context.Position));
					rawValue.Append("\"\"\"");
					context.Skip(3);
					chunkStart = context.Position;
				}
			}

			throw new SyntaxException(context, "Unterminated string.");
		}

		public string TrimBlockStringValue(string rawString)
		{
			// Expand a block string's raw value into independent lines.
			string[] lines = rawString
				.Replace("\r\n", "\n")
				.Replace("\n\r", "\n")
				.Split('\n');

			string[] trimmedLines = new string[lines.Length];

			// Remove common indentation from all lines but first.
			int commonIndent = int.MaxValue;
			for (int i = 1; i < lines.Length; i++)
			{
				trimmedLines[i] = lines[i].TrimStart(' ', '\t');
				int indent = lines[i].Length - trimmedLines[i].Length;
				if (indent < trimmedLines[i].Length
					&& indent < commonIndent)
				{
					commonIndent = indent;
					if (commonIndent == 0)
					{
						break;
					}
				}
			}

			if (commonIndent > 0)
			{
				for (int i = 1; i < lines.Length; i++)
				{
					lines[i] = lines[i].Substring(commonIndent);
				}
			}

			// Remove leading and trailing blank lines.
			int start = 0;
			for (int i = 0; i <= trimmedLines.Length; i++)
			{
				if (trimmedLines[i].Length > 0)
				{
					break;
				}
				start = i;
			}

			if (start == trimmedLines.Length - 1)
			{
				return string.Empty;
			}

			int end = trimmedLines.Length;
			for (int i = trimmedLines.Length; i >= 0; i--)
			{
				if (trimmedLines[i].Length > 0)
				{
					break;
				}
				end = i;
			}

			// Return a string of the lines joined with U+000A.
			return string.Join("\n", trimmedLines.Skip(start).Take(end - start));
		}
	}
}
