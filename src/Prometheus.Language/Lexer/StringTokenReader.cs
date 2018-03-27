using System.Text;

namespace Prometheus.Language
{
    /// <summary>
    /// Reads string tokens as specified in 
    /// http://facebook.github.io/graphql/October2016/#StringValue.
    /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
    /// </summary>
    public class StringTokenReader
        : TokenReaderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.StringTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
        public StringTokenReader(ReadNextToken readNextTokenDelegate)
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
            return context.PeekTest(c => c.IsQuote())
                && !context.PeekTest(c => c.IsQuote(), c => c.IsQuote(), c => c.IsQuote());
        }

        /// <summary>
        /// Reads a string value token from the lexer context.
        /// </summary>  
        /// <returns>
        /// Returns the string value token read from the lexer context.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
        public override Token ReadToken(ILexerContext context, Token previous)
        {
            int start = context.Position;
            int chunkStart = start + 1;
            StringBuilder value = new StringBuilder();

            // skip the opening quote
            context.Skip();

            while (context.PeekTest(c => !c.IsNewLine() || !c.IsReturn()))
            {
                // closing Quote (")
                char code = context.Read();
                if (code.IsQuote())
                {
                    value.Append(context.Read(chunkStart, context.Position - 1));
                    return CreateToken(context, previous,
                        TokenKind.String, start, value.ToString());
                }

                // SourceCharacter
                if (code.IsControlCharacter() && !code.IsTab())
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
