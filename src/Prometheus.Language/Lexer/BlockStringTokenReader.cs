using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.Language
{
	/// <summary>
	/// Reads block string tokens as specified in 
	/// http://facebook.github.io/graphql/draft/#BlockStringCharacter.
	/// </summary>
	public class BlockStringTokenReader
		: TokenReaderBase
	{
		/// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.BlockStringTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
		public BlockStringTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		/// <summary>
		/// Defines if this <see cref="ITokenReader"/> is able to 
		/// handle the next token.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if this <see cref="ITokenReader"/> is able to 
		/// handle the next token, <c>false</c> otherwise.
		/// </returns>
		/// <param name="context">The lexer context.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="context"/> is <c>null</c>.
		/// </exception>
		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(
				c => c.IsQuote(),
				c => c.IsQuote(),
				c => c.IsQuote());
		}

		/// <summary>
		/// Reads a block string token from the lexer context.
		/// </summary>  
		/// <returns>
		/// Returns the block string token read from the lexer context.
		/// </returns>
		/// <param name="context">The lexer context.</param>
		/// <param name="previous">The previous-token.</param>
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
					return CreateToken(context, previous, TokenKind.BlockString,
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
					rawValue.Append(context.Read(chunkStart, context.Position - 1));
					rawValue.Append("\"\"\"");
					context.Skip(3);
					chunkStart = context.Position;
				}
			}

			throw new SyntaxException(context, "Unterminated string.");
		}

		public string TrimBlockStringValue(string rawString)
		{
			string[] lines = ParseLines(rawString);
			string[] trimmedLines = new string[lines.Length];

			int commonIndent = DetermineCommonIdentation(lines, trimmedLines);
			RemoveCommonIndetation(lines, commonIndent);

			// Return a string of the lines joined with U+000A.
			return string.Join("\n", TrimBlankLines(lines, trimmedLines));
		}

		private int DetermineCommonIdentation(string[] lines, string[] trimmedLines)
		{
			int commonIndent = lines.Length < 2 ? 0 : int.MaxValue;
			trimmedLines[0] = lines[0].TrimStart(' ', '\t');

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

			return commonIndent;
		}

		private void RemoveCommonIndetation(string[] lines, int commonIndent)
		{
			if (commonIndent > 0)
			{
				for (int i = 1; i < lines.Length; i++)
				{
					lines[i] = lines[i].Substring(commonIndent);
				}
			}
		}

		/// <summary>
		/// Trims leading and trailing the blank lines.
		/// </summary>
		/// <returns>Returns the trimmed down lines.</returns>
		private IEnumerable<string> TrimBlankLines(string[] lines, string[] trimmedLines)
		{
			int start = 0;
			for (int i = 0; i <= trimmedLines.Length; i++)
			{
				if (trimmedLines[i].Length > 0)
				{
					break;
				}
				start++;
			}

			if (start == trimmedLines.Length - 1)
			{
				return Enumerable.Empty<string>();
			}

			int end = trimmedLines.Length;
			for (int i = trimmedLines.Length - 1; i >= 0; i--)
			{
				if (trimmedLines[i].Length > 0)
				{
					break;
				}
				end--;
			}

			if (end == trimmedLines.Length && start == 0)
			{
				return lines;
			}
			return lines.Skip(start).Take(end - start);
		}

		private string[] ParseLines(string rawString)
		{
			return rawString
				.Replace("\r\n", "\n")
				.Replace("\n\r", "\n")
				.Split('\n');
		}
	}
}
