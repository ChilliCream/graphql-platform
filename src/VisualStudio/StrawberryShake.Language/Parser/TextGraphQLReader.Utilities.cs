using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct TextGraphQLReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext()
        {
            while (Read() && _kind == TokenKind.Comment)
            { }
            return !IsEndOfStream();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Skip(TokenKind kind)
        {
            if (_kind == kind)
            {
                MoveNext();
                return true;
            }
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
                        new string(c),
                        _token);
                }
            }

            _token = current;
        }
    }
}
