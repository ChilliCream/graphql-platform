using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Prometheus.Language
{
    public class Lexer
        : ILexer
    {
        public Token Read(ISource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var context = new LexerContext(source);
            return CreateToken(context, null, TokenKind.StartOfFile, 0);
        }

        private Token ReadNextToken(ILexerContext context, Token previous)
        {
            SkipWhitespaces(context, previous);
            context.Column = 1 + context.Position - context.LineStart;

            if (context.IsEndOfStream())
            {
                return new Token(TokenKind.EndOfFile, context.Column,
                    previous.End, context.Line, context.Column,
                    previous, new Thunk<Token>(default(Token)));
            }

            char code = context.Read();

            if (code.IsPunctuator())
            {
                return ReadPunctuator(context, previous);
            }

            if (code.IsLetter() || code.IsUnderscore())
            {
                return ReadName(context, previous);
            }

            if (code.IsDigit() || code.IsMinus())
            {
                return ReadNumber(context, previous, code);
            }

            if (code.IsQuote())
            {
                if (context.PeekTest(c => c.IsQuote(), c => c.IsQuote()))
                {
                    context.Read();
                    context.Read();
                    return ReadBlockString(context, previous);
                }
                return ReadString(context, previous);
            }

            if (code.IsHash())
            {

            }


            return null;
        }

        public void SkipWhitespaces(ILexerContext context, Token previous)
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

        private Token ReadPunctuator(ILexerContext context, Token previous)
        {
            if (context.PeekTest(c => c.IsDot(), c => c.IsDot()))
            {
                throw new NotImplementedException();
            }

            TokenKind kind = LookupPunctuator(context, context.ReadPrevious());
            return CreateToken(context, previous, kind, context.Position);
        }

        private TokenKind LookupPunctuator(ILexerContext context, char code)
        {
            switch (code)
            {
                case '!': return TokenKind.Bang;
                case '$': return TokenKind.Dollar;
                case '&': return TokenKind.Ampersand;
                case '(': return TokenKind.LeftParenthesis;
                case ')': return TokenKind.RightParenthesis;
                case ':': return TokenKind.Colon;
                case '=': return TokenKind.Equal;
                case '@': return TokenKind.At;
                case '[': return TokenKind.LeftBracket;
                case ']': return TokenKind.RightBracket;
                case '{': return TokenKind.LeftBrace;
                case '|': return TokenKind.Pipe;
                case '}': return TokenKind.RightBrace;
                default:
                    throw new SyntaxException(context,
                        $"Invalid punctuator code: {code}.");
            }
        }

        private Token ReadNumber(ILexerContext context, Token previous, char firstCode)
        {
            char code = firstCode;
            int start = context.Position;
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
                code = ReadDigits(context, code);
            }

            if (code.IsDot())
            {
                isFloat = true;
                code = ReadDigits(context, context.Read());
            }

            if (code == 'E' || code == 'e')
            {
                isFloat = true;

                code = context.Read();
                if (code.IsPlus() || code.IsMinus())
                {
                    code = context.Read();
                }
                code = ReadDigits(context, code);
            }

            TokenKind kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            return CreateToken(context, previous, kind, start,
                context.Read(start, context.Position));
        }

        private char ReadDigits(ILexerContext context, char firstCode)
        {
            if (!firstCode.IsDigit())
            {
                throw new SyntaxException(context,
                    $"Invalid number, expected digit but got: {firstCode}.");
            }

            char code = firstCode;
            while (code.IsDigit())
            {
                code = context.Read();
            }
            return code;
        }

        /**
        * Reads an alphanumeric + underscore name from the source.
        *
        * [_A-Za-z][_0-9A-Za-z]*
        */
        private Token ReadName(ILexerContext context, Token previous)
        {
            int start = context.Position;

            while (context.PeekTest(c => c.IsLetterOrDigit() || c.IsUnderscore()))
            {
                context.Read();
            }

            return CreateToken(context, previous, TokenKind.Name,
                start, context.Read(start, context.Position));
        }

        /**
        * Reads a string token from the source file.
        *
        * "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
        */
        public Token ReadString(ILexerContext context, Token previous)
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

        /**
         * Reads a block string token from the source file.
         *
         * """("?"?(\\"""|\\(?!=""")|[^"\\]))*"""
         */
        public Token ReadBlockString(ILexerContext context, Token previous)
        {
            StringBuilder rawValue = new StringBuilder();
            int chunkStart = context.Position;
            int start = context.Position - 3;

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