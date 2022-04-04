using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

internal static class CollectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEqualTo<T>(this IReadOnlyList<T>? x, IReadOnlyList<T>? y)
        where T : ISyntaxNode
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        return EqualityHelper.Equals(x, y);
    }
}
