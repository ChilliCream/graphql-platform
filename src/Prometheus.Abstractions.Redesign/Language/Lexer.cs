using System;
using System.IO;

namespace Prometheus.Language
{



    public class Lexer
    {

        private string _body;
        private int _line;
        private int _lineStart;


        private TokenConfig ReadNextToken(TokenConfig previous)
        {
            int pos = GetPositionAfterWhitespace(previous);
            int line = _line;
            int col = 1 + pos - _lineStart;

            if (reader.EndOfStream)
            {
                return new TokenConfig(TokenKind.EndOfFile, pr, previous.End, line, col, previous);
            }

            int code = _body[pos];

            // SourceCharacter
            if (code < 0x0020 && code !== 0x0009 && code !== 0x000a && code !== 0x000d)
            {
                throw new SyntaxException(
                  _body, pos,
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
                    if (_body[pos + 1] == 46
                        && _body[pos + 2] == 46)
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
                    return readNumber(source, pos, code, line, col, prev);
                // "
                case 34:
                    if (_body[pos + 1] == 34
                           && _body[pos + 2] == 34)
                    {
                        return readBlockString(source, pos, line, col, prev);
                    }
                    return readString(source, pos, line, col, prev);
            }

            throw syntaxError(source, pos, unexpectedCharacterMessage(code));
        }

        private int GetPositionAfterWhitespace(TokenConfig previous)
        {

        }



        private string ReadAllSource()
        {
            throw new NotImplementedException();
        }

        private TokenConfig ReadNumber(source, start, firstCode, line, col, prev)
        {
            const body = source.body;
            let code = firstCode;
            let position = start;
            let isFloat = false;

            if (code === 45)
            {
                // -
                code = charCodeAt.call(body, ++position);
            }

            if (code === 48)
            {
                // 0
                code = charCodeAt.call(body, ++position);
                if (code >= 48 && code <= 57)
                {
                    throw syntaxError(
                      source,
                      position,
                      `Invalid number, unexpected digit after 0: ${ printCharCode(code)}.`,);
                }
            }
            else
            {
                position = readDigits(source, position, code);
                code = charCodeAt.call(body, position);
            }

            if (code === 46)
            {
                // .
                isFloat = true;

                code = charCodeAt.call(body, ++position);
                position = readDigits(source, position, code);
                code = charCodeAt.call(body, position);
            }

            if (code === 69 || code === 101)
            {
                // E e
                isFloat = true;

                code = charCodeAt.call(body, ++position);
                if (code === 43 || code === 45)
                {
                    // + -
                    code = charCodeAt.call(body, ++position);
                }
                position = readDigits(source, position, code);
            }

            return new Tok(
              isFloat ? TokenKind.FLOAT : TokenKind.INT,
              start,
              position,
              line,
              col,
              prev,
              slice.call(body, start, position),


            );
        }
    }

    [System.Serializable]
    public class SyntaxException : System.Exception
    {
        public SyntaxException(string source, int position, string message) { }
    }

    public class LexerContext
    {

    }

    public class Source
    {

    }
}