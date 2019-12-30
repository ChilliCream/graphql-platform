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
    }
}
