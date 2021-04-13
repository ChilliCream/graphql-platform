using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace StrawberryShake.VisualStudio.Language
{
    public ref partial struct StringGraphQLReader
    {

        internal bool MoveNext()
        {
            while (Read() && _kind == TokenKind.Comment)
            { }
            return !IsEndOfStream();
        }


        internal bool Skip(TokenKind kind)
        {
            if (_kind == kind)
            {
                MoveNext();
                return true;
            }
            return false;
        }

        internal bool Skip(TokenKind kind, out ISyntaxToken? token)
        {
            if (_kind == kind)
            {
                token = Token;
                MoveNext();
                return true;
            }

            token = null;
            return false;
        }

        private unsafe void CreateToken()
        {
            SyntaxToken current;

            if (_value.Length == 0)
            {
                current = new SyntaxToken(
                    _kind,
                    _start,
                    _end,
                    _line,
                    _column,
                    null,
                    _token);
            }
            else
            {
                fixed (char* c = _value)
                {
                    current = new SyntaxToken(
                        _kind,
                        _start,
                        _end,
                        _line,
                        _column,
                        new string(c, 0, _value.Length),
                        _token);
                }
            }

            _token.Next = current;
            _token = current;
        }
    }
}
