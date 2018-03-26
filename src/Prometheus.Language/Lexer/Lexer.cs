using System;
using System.Text;

namespace Prometheus.Language
{
	public class Lexer
		: ILexer
	{
		public Lexer()
		{
			Punctuator = new PunctuatorTokenReader(ReadNextToken);
			Name = new NameTokenReader(ReadNextToken);
			Number = new NumberTokenReader(ReadNextToken);
			Comment = new CommentTokenReader(ReadNextToken);
			BlockString = new BlockStringTokenReader(ReadNextToken);
		}

		private ITokenReader Punctuator { get; }
		private ITokenReader Name { get; }
		private ITokenReader Number { get; }
		private ITokenReader Comment { get; }
		private ITokenReader BlockString { get; }
		private ITokenReader String { get; }

		/// <summary>
		/// Read the first token from the given <paramref name="source"/>.
		/// </summary>
		/// <returns>
		/// Returns the first token from the given <paramref name="source"/>.
		/// </returns>
		/// <param name="source">The graphql source.</param>
		public Token Read(ISource source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			var context = new LexerContext(source);
			return CreateToken(context, null, TokenKind.StartOfFile, 0);
		}

		/// <summary>
		/// Reads the token that comes after the <paramref name="previous"/>-token.
		/// </summary>
		/// <returns>Returns token that comes after the <paramref name="previous"/>-token.</returns>
		/// <param name="context">The lexer context.</param>
		/// <param name="previous">The previous-token.</param>
		private Token ReadNextToken(ILexerContext context, Token previous)
		{
			SkipWhitespaces(context);
			context.Column = 1 + context.Position - context.LineStart;

			if (context.IsEndOfStream())
			{
				return new Token(TokenKind.EndOfFile, context.Column,
					previous.End, context.Line, context.Column,
					previous, new Thunk<Token>(default(Token)));
			}

			if (TryGetTokenReader(context, out var tokenReader))
			{
				return tokenReader.ReadToken(context, previous);
			}

			throw new SyntaxException(context, "TODO : Text");
		}

		private bool TryGetTokenReader(
			ILexerContext context,
			out ITokenReader tokenReader)
		{
			if (Punctuator.CanHandle(context))
			{
				tokenReader = Punctuator;
			}
			else if (Name.CanHandle(context))
			{
				tokenReader = Name;
			}
			else if (Number.CanHandle(context))
			{
				tokenReader = Number;
			}
			else if (Comment.CanHandle(context))
			{
				tokenReader = Comment;
			}
			else if (BlockString.CanHandle(context))
			{
				tokenReader = BlockString;
			}
			else if (String.CanHandle(context))
			{
				tokenReader = String;
			}
			else
			{
				tokenReader = null;
			}

			return tokenReader != null;
		}

		/// <summary>
		/// Skips the whitespaces.
		/// </summary>
		/// <param name="context">The lexer context.</param>
		public void SkipWhitespaces(ILexerContext context)
		{
			while (context.PeekTest(c => c.IsWhitespace()))
			{
				char code = context.Read();

				if (code.IsNewLine())
				{
					context.NewLine();
				}
				else if (code.IsReturn())
				{
					if (context.PeekTest(c => c.IsNewLine()))
					{
						context.Read();
					}
					context.NewLine();
				}
				else
				{
					break;
				}
			}
		}
  
		private Token CreateToken(ILexerContext context, Token previous,
			TokenKind kind, int start, string value)
		{
			NextTokenThunk next = CreateNextThunk(context);
			Token token = new Token(kind, start, context.Position,
				context.Line, context.Column, value, previous, next);
			next.SetPrevious(token);
			return token;
		}

		private Token CreateToken(ILexerContext context, Token previous,
			TokenKind kind, int start)
		{
			NextTokenThunk next = CreateNextThunk(context);
			Token token = new Token(kind, start, context.Position,
				context.Line, context.Column, previous, next);
			next.SetPrevious(token);
			return token;
		}

		private NextTokenThunk CreateNextThunk(ILexerContext context)
		{
			return new NextTokenThunk(previous => ReadNextToken(context, previous));
		}
	}
}