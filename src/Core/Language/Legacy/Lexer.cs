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
    public class Lexer
    {
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

            var state = new LexerState(source.Text);

            try
            {
                var start = new SyntaxToken(TokenKind.StartOfFile,
                    0, 0, state.Line, state.Column, null);

                SyntaxToken current = start;

                do
                {
                    SyntaxToken previous = current;
                    current = ReadNextToken(state, previous);
                    previous.Next = current;
                }
                while (current.Kind != TokenKind.EndOfFile);

                return start;
            }
            catch (Exception ex) when (!(ex is SyntaxException))
            {
                throw new SyntaxException(state,
                    "Unexpected token sequence.",
                    ex);
            }
        }

        /// <summary>
        /// Reads the token that comes after the
        /// <paramref name="previous"/>-token.
        /// </summary>
        /// <returns>
        /// Returns token that comes after the
        /// <paramref name="previous"/>-token.
        /// </returns>
        /// <param name="state">The lexer state.</param>
        /// <param name="previous">The previous-token.</param>
        private static SyntaxToken ReadNextToken(
            LexerState state,
            SyntaxToken previous)
        {
            SkipWhitespaces(state);
            state.UpdateColumn();

            if (state.IsEndOfStream())
            {
                return new SyntaxToken(TokenKind.EndOfFile, state.Column,
                    previous.End, state.Line, state.Column,
                    previous);
            }

            var code = state.SourceText[state.Position];

            if (code.IsLetterOrUnderscore())
            {
                return ReadNameToken(state, previous);
            }

            if (code.IsPunctuator())
            {
                return ReadPunctuatorToken(state, previous, in code);
            }

            if (code.IsDigitOrMinus())
            {
                return ReadNumberToken(state, previous, in code);
            }

            if (code.IsHash())
            {
                return ReadCommentToken(state, previous);
            }

            if (code.IsQuote())
            {
                if (state.SourceText[state.Position + 1].IsQuote()
                    && state.SourceText[state.Position + 2].IsQuote())
                {
                    state.Position += 2;
                    return ReadBlockStringToken(state, previous);
                }
                return ReadStringValueToken(state, previous);
            }

            throw new SyntaxException(state, "Unexpected character.");
        }

        /// <summary>
        /// Reads punctuator tokens as specified in
        /// http://facebook.github.io/graphql/October2016/#sec-Punctuators
        /// one of ! $ ( ) ... : = @ [ ] { | }
        /// additionaly the reader will tokenize ampersands.
        /// </summary>
        /// <param name="state">
        /// The lexer state.
        /// </param>
        /// <param name="previous">
        /// The previous-token.
        /// </param>
        /// <param name="firstCode">
        /// The first character of the punctuator.
        /// </param>
        /// <returns>
        /// Returns the punctuator token read from the current lexer state.
        /// </returns>
        private static SyntaxToken ReadPunctuatorToken(
            LexerState state,
            SyntaxToken previous,
            in char firstCode)
        {
            state.Position++;

            switch (firstCode)
            {
                case '!':
                    return CreateToken(state, previous,
                        TokenKind.Bang);
                case '$':
                    return CreateToken(state, previous,
                        TokenKind.Dollar);
                case '&':
                    return CreateToken(state, previous,
                        TokenKind.Ampersand);
                case '(':
                    return CreateToken(state, previous,
                        TokenKind.LeftParenthesis);
                case ')':
                    return CreateToken(state, previous,
                        TokenKind.RightParenthesis);
                case ':':
                    return CreateToken(state, previous,
                        TokenKind.Colon);
                case '=':
                    return CreateToken(state, previous,
                        TokenKind.Equal);
                case '@':
                    return CreateToken(state, previous,
                        TokenKind.At);
                case '[':
                    return CreateToken(state, previous,
                        TokenKind.LeftBracket);
                case ']':
                    return CreateToken(state, previous,
                        TokenKind.RightBracket);
                case '{':
                    return CreateToken(state, previous,
                        TokenKind.LeftBrace);
                case '|':
                    return CreateToken(state, previous,
                        TokenKind.Pipe);
                case '}':
                    return CreateToken(state, previous,
                        TokenKind.RightBrace);
                case '.':
                    if (state.SourceText[state.Position].IsDot()
                        && state.SourceText[state.Position + 1].IsDot())
                    {
                        state.Position += 2;
                        return CreateToken(state, previous,
                            TokenKind.Spread, state.Position - 3);
                    }

                    state.Position--;
                    throw new SyntaxException(state,
                        "Expected a spread token.");
            }

            state.Position--;
            throw new SyntaxException(state,
                "Unexpected punctuator character.");
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
        private static SyntaxToken ReadCommentToken(
            LexerState state,
            SyntaxToken previous)
        {
            var start = state.Position;

            while (++state.Position < state.SourceText.Length
                && !state.SourceText[state.Position].IsControlCharacter())
                ;

            var comment = state.SourceText.Substring(
                start, state.Position - start);
            return CreateToken(state, previous, TokenKind.Comment,
                start, comment.TrimStart('#', ' ', '\t'));
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
        private static SyntaxToken ReadNameToken(
            LexerState state,
            SyntaxToken previous)
        {
            var start = state.Position;
            var position = state.Position;

            do
            {
                position++;
            }
            while (position < state.SourceText.Length
                && state.SourceText[position].IsLetterOrDigitOrUnderscore());

            state.Position = position;

            return CreateToken(state, previous, TokenKind.Name, start,
                state.SourceText.Substring(start, state.Position - start));
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
        private static SyntaxToken ReadNumberToken(
            LexerState state,
            SyntaxToken previous,
            in char firstCode)
        {
            var start = state.Position;
            var code = firstCode;
            var isFloat = false;

            if (code.IsMinus())
            {
                code = state.SourceText[++state.Position];
            }

            if (code == '0')
            {
                if (!state.IsEndOfStream(++state.Position))
                {
                    code = state.SourceText[state.Position];
                    if (char.IsDigit(code))
                    {
                        throw new SyntaxException(state,
                            $"Invalid number, unexpected digit after 0: {code}.");
                    }
                }
            }
            else
            {
                ReadDigits(state, in code);
                code = state.Position < state.SourceText.Length
                    ? state.SourceText[state.Position]
                    : ' ';
            }

            if (code.IsDot())
            {
                isFloat = true;
                code = state.SourceText[++state.Position];
                ReadDigits(state, in code);
                code = state.Position < state.SourceText.Length
                    ? state.SourceText[state.Position]
                    : ' ';
            }

            code |= (char)0x20;
            if (code == 'e') // shift instead of or
            {
                isFloat = true;
                code = state.SourceText[++state.Position];
                if (code.IsPlus() || code.IsMinus())
                {
                    code = state.SourceText[++state.Position];
                }
                ReadDigits(state, in code);
            }

            TokenKind kind = isFloat ? TokenKind.Float : TokenKind.Integer;
            return CreateToken(state, previous, kind, start,
                state.SourceText.Substring(start, state.Position - start));
        }

        private static void ReadDigits(LexerState state, in char firstCode)
        {
            if (!firstCode.IsDigit())
            {
                throw new SyntaxException(state,
                    $"Invalid number, expected digit but got: {firstCode}.");
            }

            while (++state.Position < state.SourceText.Length
                && state.SourceText[state.Position].IsDigit())
            { }
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
        private static SyntaxToken ReadBlockStringToken(
            LexerState state, SyntaxToken previous)
        {
            var rawValue = new StringBuilder();
            var start = state.Position - 2;
            var chunkStart = state.Position + 1;

            while (!state.IsEndOfStream())
            {
                var code = state.SourceText[++state.Position];

                // Closing Triple-Quote (""")
                if (code.IsQuote()
                    && state.SourceText[++state.Position].IsQuote()
                    && state.SourceText[++state.Position].IsQuote())
                {
                    var length = state.Position - chunkStart - 2;
                    if (length > 0)
                    {
                        rawValue.Append(state.SourceText
                            .Substring(chunkStart, length));
                    }

                    (string value, int lines) result =
                        TrimBlockStringValue(rawValue.ToString());
                    SyntaxToken token = CreateToken(state, previous,
                        TokenKind.BlockString, start, result.value);
                    state.Position++;
                    if (result.lines > 1)
                    {
                        state.NewLine(result.lines - 1);
                    }
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
                    && state.SourceText[++state.Position].IsQuote()
                    && state.SourceText[++state.Position].IsQuote()
                    && state.SourceText[++state.Position].IsQuote())
                {
                    var length = state.Position - chunkStart - 3;
                    if (length > 0)
                    {
                        rawValue.Append(state.SourceText
                            .Substring(chunkStart, length));
                    }
                    rawValue.Append("\"\"\"");
                    chunkStart = state.Position + 1;
                }
            }

            throw new SyntaxException(state, "Unterminated string.");
        }

        private static (string value, int lines) TrimBlockStringValue(
            string rawString)
        {
            var lines = rawString.Split('\n');
            var trimmedLines = new string[lines.Length];

            var commonIndent = DetermineCommonIdentation(lines, trimmedLines);
            RemoveCommonIndetation(lines, in commonIndent);

            // Return a string of the lines joined with U+000A.
            return (string.Join("\n", TrimBlankLines(lines, trimmedLines)),
                lines.Length);
        }

        private static int DetermineCommonIdentation(
            string[] lines,
            string[] trimmedLines)
        {
            var commonIndent = lines.Length < 2 ? 0 : int.MaxValue;
            trimmedLines[0] = lines[0].TrimStart(' ', '\t');

            for (var i = 1; i < lines.Length; i++)
            {
                trimmedLines[i] = lines[i].TrimStart(' ', '\t');
                var indent = lines[i].Length - trimmedLines[i].Length;
                if (indent >= 0 && indent < commonIndent)
                {
                    commonIndent = indent;
                }
            }

            return commonIndent;
        }

        private static void RemoveCommonIndetation(
            string[] lines, in int commonIndent)
        {
            if (commonIndent > 0)
            {
                for (var i = 1; i < lines.Length; i++)
                {
                    lines[i] = lines[i].Substring(commonIndent);
                }
            }
        }

        /// <summary>
        /// Trims leading and trailing the blank lines.
        /// </summary>
        /// <returns>Returns the trimmed down lines.</returns>
        private static IEnumerable<string> TrimBlankLines(
            string[] lines,
            string[] trimmedLines)
        {
            var start = 0;
            for (var i = 0; i <= trimmedLines.Length; i++)
            {
                if (trimmedLines[i]?.Length > 0)
                {
                    break;
                }
                start++;
            }

            if (start > 0 && start == trimmedLines.Length - 1)
            {
                return Enumerable.Empty<string>();
            }

            var end = trimmedLines.Length;
            for (var i = trimmedLines.Length - 1; i >= 0; i--)
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
        private static SyntaxToken ReadStringValueToken(
            LexerState state,
            SyntaxToken previous)
        {
            var start = state.Position;
            var chunkStart = state.Position + 1;
            var value = new StringBuilder();

            char code;
            while (!(code = state.SourceText[++state.Position]).IsNewLine())
            {
                // closing Quote (")
                if (code.IsQuote())
                {
                    value.Append(state.SourceText.Substring(
                        chunkStart, state.Position - chunkStart));
                    SyntaxToken token = CreateToken(state, previous,
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
                    value.Append(state.SourceText.Substring(
                        chunkStart, state.Position - chunkStart));
                    value.Append(ReadEscapedChar(state));
                    chunkStart = state.Position + 1;
                }
            }

            throw new SyntaxException(state, "Unterminated string.");
        }

        private static char ReadEscapedChar(LexerState state)
        {
            var code = state.SourceText[++state.Position];

            if (code.IsValidEscapeCharacter())
            {
                return code.EscapeCharacter();
            }

            if (code == 'u')
            {
                if (!TryReadUnicodeChar(state, out code))
                {
                    var start = state.Position - 4;
                    var escapeChar = state.SourceText
                        .Substring(start, state.Position - start);
                    throw new SyntaxException(state,
                        "Invalid character escape sequence: " +
                        $"\\u{escapeChar}.");
                }
                return code;
            }

            throw new SyntaxException(state,
                $"Invalid character escape sequence: \\{code}.");
        }

        private static bool TryReadUnicodeChar(LexerState state, out char code)
        {
            var c = (CharToHex(state.SourceText[++state.Position]) << 12)
                | (CharToHex(state.SourceText[++state.Position]) << 8)
                | (CharToHex(state.SourceText[++state.Position]) << 4)
                | CharToHex(state.SourceText[++state.Position]);

            if (c < 0)
            {
                code = default;
                return false;
            }

            code = (char)c;
            return true;
        }

        private static int CharToHex(int a)
        {
            return a >= 48 && a <= 57
              ? a - 48 // 0-9
              : a >= 65 && a <= 70
                ? a - 55 // A-F
                : a >= 97 && a <= 102
                  ? a - 87 // a-f
                  : -1;
        }

        private static SyntaxToken CreateToken(
            LexerState state,
            SyntaxToken previous,
            TokenKind kind)
        {
            return new SyntaxToken(kind, state.Position - 1, state.Position,
                state.Line, state.Column, previous);
        }

        private static SyntaxToken CreateToken(
            LexerState state,
            SyntaxToken previous,
            TokenKind kind,
            int start)
        {
            return new SyntaxToken(kind, start, state.Position,
                state.Line, state.Column, previous);
        }

        private static SyntaxToken CreateToken(
            LexerState state,
            SyntaxToken previous,
            TokenKind kind,
            int start,
            string value)
        {
            return new SyntaxToken(kind, start, state.Position,
                state.Line, state.Column, value, previous);
        }


        /// <summary>
        /// Skips the whitespaces and moves the position
        /// to the next non whitespace character.
        /// </summary>
        private static void SkipWhitespaces(LexerState state)
        {
            if (state.IsEndOfStream())
            {
                return;
            }

            char code = state.SourceText[state.Position];
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

                code = state.SourceText[state.Position];
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
