using System;
using System.IO;

namespace Prometheus.Language
{
    public class LexerState
    {
        public Source Source { get; }
        public int Line { get; }
        public int LineStart { get; }
    }

    public class Source
    {
        private readonly string _body;

        public Source(string body)
        {
            _body = body;
        }


        public char Read(int position)
        {
            return _body[position];
        }

        public string Read(int startIndex, int length)
        {
            return _body.Substring(startIndex, length);
        }

        public bool Check(int startIndex, params int[] expectedCharacters)
        {
            for (int i = 0; i < expectedCharacters.Length; i++)
            {
                if (_body[i + startIndex] != expectedCharacters[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsEndOfStream(int position)
        {
            return (position <= _body.Length);
        }
    }



    public class Lexer
    {
        public TokenConfig ReadNextToken(LexerState state, Source source, TokenConfig previous)
        {
            int pos = GetPositionAfterWhitespace(previous);
            int line = state.Line;
            int col = 1 + pos - state.LineStart;

            if (source.IsEndOfStream(pos))
            {
                return new TokenConfig(TokenKind.EndOfFile, pos, previous.End, line, col, previous);
            }

            int code = source.Read(pos);

            // SourceCharacter
            if (code < 0x0020 && code != 0x0009 && code != 0x000a && code != 0x000d)
            {
                throw new SyntaxException(
                  source, pos,
                  "Cannot contain the invalid character ${printCharCode(code)}.");
            }

            switch (code)
            {
                // !
                case 33:
                    return new TokenConfig(TokenKind.Bang, pos, pos + 1, line, col, previous);
                // #
                case 35:
                    return readComment(source, pos, line, col, previous);
                // $
                case 36:
                    return new TokenConfig(TokenKind.Dollar, pos, pos + 1, line, col, previous);
                // &
                case 38:
                    return new TokenConfig(TokenKind.Ampersand, pos, pos + 1, line, col, previous);
                // (
                case 40:
                    return new TokenConfig(TokenKind.LeftParenthesis, pos, pos + 1, line, col, previous);
                // )
                case 41:
                    return new TokenConfig(TokenKind.RightParenthesis, pos, pos + 1, line, col, previous);
                // .
                case 46:
                    if (source.Check(pos + 1, 46, 46))
                    {
                        return new TokenConfig(TokenKind.Spread, pos, pos + 3, line, col, previous);
                    }
                    break;
                // :
                case 58:
                    return new TokenConfig(TokenKind.Colon, pos, pos + 1, line, col, previous);
                // =
                case 61:
                    return new TokenConfig(TokenKind.Equal, pos, pos + 1, line, col, previous);
                // @
                case 64:
                    return new TokenConfig(TokenKind.At, pos, pos + 1, line, col, previous);
                // [
                case 91:
                    return new TokenConfig(TokenKind.LeftBracket, pos, pos + 1, line, col, previous);
                // ]
                case 93:
                    return new TokenConfig(TokenKind.RightBracket, pos, pos + 1, line, col, previous);
                // {
                case 123:
                    return new TokenConfig(TokenKind.LeftBrace, pos, pos + 1, line, col, previous);
                // |
                case 124:
                    return new TokenConfig(TokenKind.Pipe, pos, pos + 1, line, col, previous);
                // }
                case 125:
                    return new TokenConfig(TokenKind.RightBrace, pos, pos + 1, line, col, previous);
                // A-Z _ a-z
                case 65:
                case 66:
                case 67:
                case 68:
                case 69:
                case 70:
                case 71:
                case 72:
                case 73:
                case 74:
                case 75:
                case 76:
                case 77:
                case 78:
                case 79:
                case 80:
                case 81:
                case 82:
                case 83:
                case 84:
                case 85:
                case 86:
                case 87:
                case 88:
                case 89:
                case 90:
                case 95:
                case 97:
                case 98:
                case 99:
                case 100:
                case 101:
                case 102:
                case 103:
                case 104:
                case 105:
                case 106:
                case 107:
                case 108:
                case 109:
                case 110:
                case 111:
                case 112:
                case 113:
                case 114:
                case 115:
                case 116:
                case 117:
                case 118:
                case 119:
                case 120:
                case 121:
                case 122:
                    return readName(source, pos, line, col, prev);
                // - 0-9
                case 45:
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                    return ReadNumber(source, pos, (char)code, line, col, previous);
                // "
                case 34:
                    if (source.Check(pos + 1, 34, 34))
                    {
                        return readBlockString(source, pos, line, col, previous);
                    }
                    return readString(source, pos, line, col, previous);
            }

            throw new SyntaxException(source, pos, "unexpectedCharacterMessage(code)");
        }

        private int GetPositionAfterWhitespace(TokenConfig previous)
        {

        }


        private TokenConfig ReadNumber(Source source, int start, char firstCode, int line, int col, TokenConfig prev)
        {
            char code = firstCode;
            int position = start;
            bool isFloat = false;

            if (code == 45)
            {
                // -
                code = source.Read(++position);
            }

            if (code == 48)
            {
                // 0
                code = source.Read(++position);
                if (code >= 48 && code <= 57)
                {
                    throw new SyntaxException(
                      source,
                      position,
                      "Invalid number, unexpected digit after 0: ${ printCharCode(code)}.`,)");
                }
            }
            else
            {
                position = ReadDigits(source, position, code);
                code = source.Read(position);
            }

            if (code == 46)
            {
                // .
                isFloat = true;

                code = source.Read(++position);
                position = ReadDigits(source, position, code);
                code = source.Read(position);
            }

            if (code == 69 || code == 101)
            {
                // E e
                isFloat = true;

                code = source.Read(++position);
                if (code == 43 || code == 45)
                {
                    // + -
                    code = source.Read(++position);
                }
                position = ReadDigits(source, position, code);
            }

            return new TokenConfig(
              isFloat ? TokenKind.Float : TokenKind.Integer,
              start,
              position,
              line,
              col,
              prev,
              source.Read(start, position));
        }

        public int ReadDigits(Source source, int start, char firstCode)
        {
            int position = start;
            char code = firstCode;

            if (code >= 48 && code <= 57)
            {
                // 0 - 9
                do
                {
                    code = source.Read(++position);
                } while (code >= 48 && code <= 57); // 0 - 9
                return position;
            }

            throw new SyntaxException(
              source,
              position,
              "Invalid number, expected digit but got: ${ printCharCode(code) }.");
        }


        /**
         * Reads a string token from the source file.
         *
         * "([^"\\\u000A\u000D]|(\\(u[0-9a-fA-F]{4}|["\\/bfnrt])))*"
         */
        public TokenConfig readString(Source source, int start, int line, int col, TokenConfig prev)
        {
            int position = start + 1;
            int chunkStart = position;
            int code = 0;
            string value = string.Empty;

            while (
              position < body.length &&
              (code = charCodeAt.call(body, position)) != null &&
              // not LineTerminator
              code != 0x000a &&
              code != 0x000d
            )
            {
                // Closing Quote (")
                if (code == 34)
                {
                    value += source.Read(chunkStart, position);
                    return new TokenConfig(
                      TokenKind.String,
                      start,
                      position + 1,
                      line,
                      col,
                      prev,
                      value);
                }

                // SourceCharacter
                if (code < 0x0020 && code != 0x0009)
                {
                    throw new SyntaxException(
                      source,
                      position,
                      "Invalid character within String: ${ printCharCode(code)}.");
                }

                ++position;
                if (code == 92)
                {
                    // \
                    value += source.Read(chunkStart, position - 1);
                    code = source.Read(position);
                    switch (code)
                    {
                        case 34:
                            value += '"';
                            break;
                        case 47:
                            value += '/';
                            break;
                        case 92:
                            value += '\\';
                            break;
                        case 98:
                            value += '\b';
                            break;
                        case 102:
                            value += '\f';
                            break;
                        case 110:
                            value += '\n';
                            break;
                        case 114:
                            value += '\r';
                            break;
                        case 116:
                            value += '\t';
                            break;
                        case 117: // u
                            int charCode = uniCharCode(
                              source.Read(position + 1),
                              source.Read(position + 2),
                              source.Read(position + 3),
                              source.Read(position + 4));

                            if (charCode < 0)
                            {
                                throw new SyntaxException(
                                  source,
                                  position,
                                  "Invalid character escape sequence: " +
                                    "\\u${ body.slice(position + 1, position + 5)}.");
                            }
                            value += (char)charCode;
                            position += 4;
                            break;
                        default:
                            throw new SyntaxException(
                                  source,
                                  position,
                              "Invalid character escape sequence: \\${String.fromCharCode(code)}.");
                    }
                    ++position;
                    chunkStart = position;
                }
            }

            throw new SyntaxException(source, position, "Unterminated string.");
        }

        /**
         * Reads a block string token from the source file.
         *
         * """("?"?(\\"""|\\(?!=""")|[^"\\]))*"""
         */
        public TokenConfig readBlockString(Source source, int start, int line, int col, TokenConfig prev)
        {
            int position = start + 3;
            int chunkStart = position;
            int code = 0;
            string rawValue = string.Empty;

            while (
              !source.IsEndOfStream(position)
            )
            {
                // Closing Triple-Quote (""")
                if (
                  code == 34 &&
                  source.Read(position + 1) == 34 &&
                  source.Read(position + 2) == 34
                )
                {
                    rawValue += source.Read(chunkStart, position);
                    return new TokenConfig(
                      TokenKind.BlockString,
                      start,
                      position + 3,
                      line,
                      col,
                      prev,
                      blockStringValue(rawValue)
                    );
                }

                // SourceCharacter
                if (
                  code < 0x0020 &&
                  code != 0x0009 &&
                  code != 0x000a &&
                  code != 0x000d
                )
                {
                    throw new SyntaxException(
                      source,
                      position,
                      "Invalid character within String: ${ printCharCode(code)}.");
                }

                // Escape Triple-Quote (\""")
                if (
                  code == 92 &&
                  source.Read(position + 1) == 34 &&
                  source.Read(position + 2) == 34 &&
                  source.Read(position + 3) == 34
                )
                {
                    rawValue += source.Read(chunkStart, position) + "\"\"\"";
                    position += 4;
                    chunkStart = position;
                }
                else
                {
                    ++position;
                }
            }

            throw new SyntaxException(source, position, "Unterminated string.");
        }

        /**
         * Converts four hexidecimal chars to the integer that the
         * string represents. For example, uniCharCode('0','0','0','f')
         * will return 15, and uniCharCode('0','0','f','f') returns 255.
         *
         * Returns a negative number on error, if a char was invalid.
         *
         * This is implemented by noting that char2hex() returns -1 on error,
         * which means the result of ORing the char2hex() will also be negative.
         */
        public int uniCharCode(int a, int b, int c, int d)
        {
            return (
              (char2hex(a) << 12) | (char2hex(b) << 8) | (char2hex(c) << 4) | char2hex(d)
            );
        }

        /**
         * Converts a hex character to its integer value.
         * '0' becomes 0, '9' becomes 9
         * 'A' becomes 10, 'F' becomes 15
         * 'a' becomes 10, 'f' becomes 15
         *
         * Returns -1 on error.
         */
        public int char2hex(int a)
        {
            return a >= 48 && a <= 57
              ? a - 48 // 0-9
              : a >= 65 && a <= 70
                ? a - 55 // A-F
                : a >= 97 && a <= 102
                  ? a - 87 // a-f
                  : -1;
        }

        /**
         * Reads an alphanumeric + underscore name from the source.
         *
         * [_A-Za-z][_0-9A-Za-z]*
         */
        public TokenConfig readName(Source source, int start, int line, int col, TokenConfig prev)
        {
            int position = start + 1;
            int code = 0;
            while (
              source.IsEndOfStream(position) &&
              (code = source.Read(position)) != null &&
              (code == 95 || // _
              (code >= 48 && code <= 57) || // 0-9
              (code >= 65 && code <= 90) || // A-Z
                (code >= 97 && code <= 122)) // a-z
            )
            {
                ++position;
            }
            return new TokenConfig(
              TokenKind.Name,
              start,
              position,
              line,
              col,
              prev,
              source.Read(start, position)


            );
        }

    }


    [System.Serializable]
    public class SyntaxException : System.Exception
    {
        public SyntaxException(Source source, int position, string message) { }
    }
}