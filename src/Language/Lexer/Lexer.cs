using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents the GraphQL lexer.
    /// The lexer tokenizes a GraphQL <see cref="ISource" />
    /// and returns the first token.
    /// The tokens are chained as a a doubly linked syntax token chain.
    /// </summary>
    public partial class Lexer
    {
        private char[] _trimCommentChars = new[] { '#', ' ', '\t' };
        private readonly char emptyString = ' ';

        /// <summary>
        /// Reads <see cref="SyntaxToken" />s from a GraphQL
        /// <paramref name="source" /> and returns the first token.
        /// </summary>
        /// <param name="source">
        /// The GraphQL source that shall be tokenized.
        /// </param>
        /// <returns>
        /// Returns the first token of the given
        /// GraphQL <paramref name="source" />.
        /// </returns>
        /// <exception cref="SyntaxException">
        /// There are unexpected tokens in the given <paramref name="source" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is null.
        /// </exception>
        public SyntaxToken Read(ISource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            LexerState state = new LexerState(source.Text);

            try
            {
                SyntaxToken start = new SyntaxToken(
                    TokenKind.StartOfFile,
                    new Range(0, 0),
                    new Position(state.Line, state.Column),
                    null);

                SyntaxToken current = start;

                do
                {
                    SyntaxToken previous = current;
                    current = ReadNextToken(ref state, previous);
                    previous.Next = current;
                }
                while (current.Kind != TokenKind.EndOfFile);

                return start;
            }
            catch (Exception ex)
            {
                throw new SyntaxException(state,
                    "Unexpected token sequence.", ex);
            }
        }

        /// <summary>
        /// Reads the token that comes after the <paramref name="previous"/>-token.
        /// </summary>
        /// <returns>Returns token that comes after the <paramref name="previous"/>-token.</returns>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        private SyntaxToken ReadNextToken(ref LexerState state, SyntaxToken previous)
        {
            SkipWhitespaces(ref state);
            state.UpdateColumn();

            if (state.IsEndOfStream())
            {
                return new SyntaxToken(TokenKind.EndOfFile,
                    new Range(state.Column, previous.End),
                    new Position(state.Line, state.Column),
                    previous);
            }

            ref readonly char code = ref state._SourceText[state.Position];

            if (code.IsLetterOrUnderscore())
            {
                return ReadNameToken(ref state, previous);
            }

            if (code.IsPunctuator())
            {
                return ReadPunctuatorToken(ref state, previous, in code);
            }

            if (code.IsDigitOrMinus())
            {
                return ReadNumberToken(ref state, previous, in code);
            }

            if (code.IsHash())
            {
                return ReadCommentToken(ref state, previous);
            }

            if (code.IsQuote())
            {
                if (state._SourceText[state.Position + 1].IsQuote()
                    && state._SourceText[state.Position + 2].IsQuote())
                {
                    state.Position += 2;
                    return ReadBlockStringToken(ref state, previous);
                }
                return ReadStringValueToken(ref state, previous);
            }

            throw new SyntaxException(state, "Unexpected character.");
        }

        /// <summary>
        /// Reads punctuator tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#sec-Punctuators
        /// one of ! $ ( ) ... : = @ [ ] { | }
        /// additionaly the reader will tokenize ampersands.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <param name="firstCode">The first character of the punctuator.</param>
        /// <returns>
        /// Returns the punctuator token read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadPunctuatorToken(
            ref LexerState state,
            SyntaxToken previous,
            in char firstCode)
        {
            state.Position++;

            switch (firstCode)
            {
                case '!': return CreateToken(ref state, previous, TokenKind.Bang);
                case '$': return CreateToken(ref state, previous, TokenKind.Dollar);
                case '&': return CreateToken(ref state, previous, TokenKind.Ampersand);
                case '(': return CreateToken(ref state, previous, TokenKind.LeftParenthesis);
                case ')': return CreateToken(ref state, previous, TokenKind.RightParenthesis);
                case ':': return CreateToken(ref state, previous, TokenKind.Colon);
                case '=': return CreateToken(ref state, previous, TokenKind.Equal);
                case '@': return CreateToken(ref state, previous, TokenKind.At);
                case '[': return CreateToken(ref state, previous, TokenKind.LeftBracket);
                case ']': return CreateToken(ref state, previous, TokenKind.RightBracket);
                case '{': return CreateToken(ref state, previous, TokenKind.LeftBrace);
                case '|': return CreateToken(ref state, previous, TokenKind.Pipe);
                case '}': return CreateToken(ref state, previous, TokenKind.RightBrace);
                case '.':
                    if (state._SourceText[state.Position].IsDot()
                       && state._SourceText[state.Position + 1].IsDot())
                    {
                        state.Position += 2;
                        return CreateToken(ref state, previous, TokenKind.Spread, state.Position - 3);
                    }

                    state.Position--;
                    throw new SyntaxException(state, "Expected a spread token.");
            }

            state.Position--;
            throw new SyntaxException(state, "Unexpected punctuator character.");
        }

        /// <summary>
        /// Reads comment tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#sec-Comments
        /// #[\u0009\u0020-\uFFFF]*
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the comment token read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadCommentToken(ref LexerState state, SyntaxToken previous)
        {
            int start = state.Position;

            while (++state.Position < state._SourceText.Length
                && !state._SourceText[state.Position].IsControlCharacter()) { }

            ReadOnlySpan<char> comment = state._SourceText
                .Slice(start, state.Position - start);
            comment = comment.TrimStart(_trimCommentChars.AsSpan());
            return CreateToken(ref state, previous, TokenKind.Comment,
                start, ref comment);
        }

        /// <summary>
        /// Reads name tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#Name
        /// [_A-Za-z][_0-9A-Za-z]
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the name token read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadNameToken(ref LexerState state, SyntaxToken previous)
        {
            int start = state.Position;

            int position = state.Position;
            do
            {
                position++;
            }
            while (position < state._SourceText.Length
                && state._SourceText[position].IsLetterOrDigitOrUnderscore());

            state.Position = position;

            ReadOnlySpan<char> name = state._SourceText
                .Slice(start, state.Position - start);
            return CreateToken(ref state, previous, TokenKind.Name,
                start, ref name);
        }

        /// <summary>
        /// Reads int tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#IntValue
        /// or a float tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#FloatValue
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <param name="firstCode">
        /// The first character of the int or float token.
        /// </param>
        /// <returns>
        /// Returns the int or float tokens read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadNumberToken(ref LexerState state, SyntaxToken previous, in char firstCode)
        {
            int start = state.Position;
            ref readonly char code = ref firstCode;
            bool isFloat = false;

            if (code.IsMinus())
            {
                code = ref state._SourceText[++state.Position];
            }

            if (code == '0')
            {
                code = ref state._SourceText[++state.Position];
                if (char.IsDigit(code))
                {
                    throw new SyntaxException(state,
                        $"Invalid number, unexpected digit after 0: {code}.");
                }
            }
            else
            {
                ReadDigits(ref state, in code);
                code = ref state.Position < state._SourceText.Length
                    ? ref state._SourceText[state.Position]
                    : ref emptyString;
            }

            if (code.IsDot())
            {
                isFloat = true;
                code = ref state._SourceText[++state.Position];
                ReadDigits(ref state, in code);
                code = ref state.Position < state._SourceText.Length
                    ? ref state._SourceText[state.Position]
                    : ref emptyString;
            }

            if (code == 'e' || code == 'E')
            {
                isFloat = true;
                code = ref state._SourceText[++state.Position];
                if (code.IsPlus() || code.IsMinus())
                {
                    code = ref state._SourceText[++state.Position];
                }
                ReadDigits(ref state, in code);
            }

            TokenKind kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            ReadOnlySpan<char> number = state._SourceText
                .Slice(start, state.Position - start);
            return CreateToken(ref state, previous, kind, start, ref number);
        }

        private void ReadDigits(ref LexerState state, in char firstCode)
        {
            if (!firstCode.IsDigit())
            {
                throw new SyntaxException(state,
                    $"Invalid number, expected digit but got: {firstCode}.");
            }

            while (++state.Position < state._SourceText.Length
                && state._SourceText[state.Position].IsDigit()) { }
        }

        /// <summary>
        /// Reads block string tokens as specified in
        /// http://facebook.github.io/graphql/draft/#BlockStringCharacter
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the block string token read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadBlockStringToken(ref LexerState state, SyntaxToken previous)
        {
            StringBuilder rawValue = new StringBuilder();
            int start = state.Position - 2;
            int chunkStart = state.Position + 1;

            while (!state.IsEndOfStream())
            {
                char code = state._SourceText[++state.Position];

                // Closing Triple-Quote (""")
                if (code.IsQuote()
                    && state._SourceText[++state.Position].IsQuote()
                    && state._SourceText[++state.Position].IsQuote())
                {
                    int length = state.Position - chunkStart - 2;
                    if (length > 0)
                    {
                        rawValue.Append(state._SourceText
                            .Slice(chunkStart, length).ToArray());
                    }

                    var result = TrimBlockStringValue(rawValue.ToString());
                    SyntaxToken token = CreateToken(ref state, previous,
                        TokenKind.BlockString, start, result.value);
                    state.Position++;
                    state.NewLine(result.lines - 1);
                    return token;
                }

                // SourceCharacter
                if (code.IsControlCharacter()
                    && !code.IsNewLine()
                    && !code.IsReturn())
                {
                    throw new SyntaxException(state,
                        $"Invalid character within String: ${code}.");
                }

                // Escape Triple-Quote (\""")
                if (code.IsBackslash()
                    && state._SourceText[++state.Position].IsQuote()
                    && state._SourceText[++state.Position].IsQuote()
                    && state._SourceText[++state.Position].IsQuote())
                {
                    int length = state.Position - chunkStart - 3;
                    if (length > 0)
                    {
                        rawValue.Append(state._SourceText.Slice(chunkStart, length).ToArray());
                    }
                    rawValue.Append("\"\"\"");
                    chunkStart = state.Position + 1;
                }
            }

            throw new SyntaxException(state, "Unterminated string.");
        }

        private (string value, int lines) TrimBlockStringValue(string rawString)
        {
            string[] lines = rawString.Split('\n');
            string[] trimmedLines = new string[lines.Length];

            int commonIndent = DetermineCommonIdentation(lines, trimmedLines);
            RemoveCommonIndetation(lines, in commonIndent);

            // Return a string of the lines joined with U+000A.
            return (string.Join("\n", TrimBlankLines(lines, trimmedLines)), lines.Length);
        }

        private int DetermineCommonIdentation(string[] lines, string[] trimmedLines)
        {
            int commonIndent = lines.Length < 2 ? 0 : int.MaxValue;
            trimmedLines[0] = lines[0].TrimStart(' ', '\t');

            for (int i = 1; i < lines.Length; i++)
            {
                trimmedLines[i] = lines[i].TrimStart(' ', '\t');
                int indent = lines[i].Length - trimmedLines[i].Length;
                if (indent >= 0 && indent < commonIndent)
                {
                    commonIndent = indent;
                }
            }

            return commonIndent;
        }

        private void RemoveCommonIndetation(string[] lines, in int commonIndent)
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
                if (trimmedLines[i]?.Length > 0)
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
                if (trimmedLines[i]?.Length > 0)
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

        /// <summary>
        /// Reads string tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#StringValue
        /// "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
        /// from the current lexer state.
        /// </summary>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        /// <returns>
        /// Returns the string value token read from the current lexer state.
        /// </returns>
        private SyntaxToken ReadStringValueToken(ref LexerState state, SyntaxToken previous)
        {
            int start = state.Position;
            int chunkStart = state.Position + 1;
            StringBuilder value = new StringBuilder();

            char code;
            while (!(code = state._SourceText[++state.Position]).IsNewLine())
            {
                // closing Quote (")
                if (code.IsQuote())
                {
                    value.Append(state._SourceText.Slice(chunkStart, state.Position - chunkStart).ToArray());
                    SyntaxToken token = CreateToken(ref state, previous,
                        TokenKind.String, start, value.ToString());
                    state.Position++;
                    return token;
                }

                // SourceCharacter
                if (code.IsControlCharacter())
                {
                    throw new SyntaxException(state,
                        $"Invalid character within String: {code}.");
                }

                if (code.IsBackslash())
                {
                    value.Append(state._SourceText.Slice(chunkStart, state.Position - chunkStart).ToArray());
                    value.Append(ReadEscapedChar(ref state));
                    chunkStart = state.Position + 1;
                }
            }

            throw new SyntaxException(state, "Unterminated string.");
        }

        private char ReadEscapedChar(ref LexerState state)
        {
            char code = state._SourceText[++state.Position];

            if (code.IsValidEscapeCharacter())
            {
                return code.EscapeCharacter();
            }

            if (code == 'u')
            {
                if (!TryReadUnicodeChar(ref state, out code))
                {
                    int start = state.Position - 4;
                    string text = new string(state._SourceText
                        .Slice(start, state.Position - start)
                        .ToArray());
                    throw new SyntaxException(state,
                        "Invalid character escape sequence: " +
                        $"\\u{text}.");
                }
                return code;
            }

            throw new SyntaxException(state,
                $"Invalid character escape sequence: \\{code}.");
        }

        private bool TryReadUnicodeChar(ref LexerState state, out char code)
        {
            int c = (CharToHex(state._SourceText[++state.Position]) << 12)
                | (CharToHex(state._SourceText[++state.Position]) << 8)
                | (CharToHex(state._SourceText[++state.Position]) << 4)
                | CharToHex(state._SourceText[++state.Position]);

            if (c < 0)
            {
                code = default(char);
                return false;
            }

            code = (char)c;
            return true;
        }

        private int CharToHex(int a)
        {
            return a >= 48 && a <= 57
              ? a - 48 // 0-9
              : a >= 65 && a <= 70
                ? a - 55 // A-F
                : a >= 97 && a <= 102
                  ? a - 87 // a-f
                  : -1;
        }

        private SyntaxToken CreateToken(ref LexerState state, SyntaxToken previous, TokenKind kind)
        {
            return new SyntaxToken(
                kind, new Range(state.Position - 1, state.Position),
                new Position(state.Line, state.Column),
                previous);
        }

        private SyntaxToken CreateToken(ref LexerState state, SyntaxToken previous, TokenKind kind, int start)
        {
            return new SyntaxToken(
                kind, new Range(start, state.Position),
                new Position(state.Line, state.Column),
                previous);
        }

        private SyntaxToken CreateToken(ref LexerState state, SyntaxToken previous, TokenKind kind, int start, ref ReadOnlySpan<char> value)
        {
            return new SyntaxToken(
                kind, new Range(start, state.Position),
                new Position(state.Line, state.Column),
                new string(value.ToArray()), previous);
        }

        private SyntaxToken CreateToken(ref LexerState state, SyntaxToken previous, TokenKind kind, int start, string value)
        {
            return new SyntaxToken(
                kind, new Range(start, state.Position),
                new Position(state.Line, state.Column),
                new string(value.ToArray()), previous);
        }


        /// <summary>
        /// Skips the whitespaces and moves the position
        /// to the next non whitespace character.
        /// </summary>
        private void SkipWhitespaces(ref LexerState state)
        {
            if (state.IsEndOfStream())
            {
                return;
            }

            ref readonly char code = ref state._SourceText[state.Position];
            while (code.IsWhitespace())
            {
                if (code.IsNewLine())
                {
                    state.NewLine();
                }

                ++state.Position;
                if (state.IsEndOfStream())
                {
                    return;
                }

                code = ref state._SourceText[state.Position];
            }
        }

        /// <summary>
        /// Gets the default instance of this lexer.
        /// </summary>
        /// <returns>
        /// Returns the default instancde of this lexer.
        /// </returns>
        public static Lexer Default { get; } = new Lexer();
    }
}
