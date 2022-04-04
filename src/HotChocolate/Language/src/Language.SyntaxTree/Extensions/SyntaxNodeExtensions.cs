using System;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

internal static class SyntaxNodeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEqualTo(this ISyntaxNode? x, ISyntaxNode? y)
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        return x.Equals(y);
    }
}
