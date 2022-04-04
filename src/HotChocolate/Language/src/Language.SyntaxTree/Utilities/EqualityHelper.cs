using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language.Utilities;

internal static class EqualityHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals<T>(IReadOnlyList<T> a, IReadOnlyList<T> b) where T : ISyntaxNode
    {
        if (a.Count == 0 && b.Count == 0)
        {
            return true;
        }

        return a.SequenceEqual(b, SyntaxEqualityComparer<T>.Default);
    }

    public static int GetHashCode<T>(IReadOnlyList<T> list) where T : ISyntaxNode
    {
        unchecked
        {
            var hashCode = 0;

            for (var i = 0; i < list.Count; i++)
            {
                hashCode = (hashCode * 397) ^ list[i].GetHashCode();
            }

            return hashCode;
        }
    }

    private class SyntaxEqualityComparer<T> : IEqualityComparer<T?> where T : ISyntaxNode
    {
        public bool Equals(T? x, T? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

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

        public int GetHashCode(T? obj) => obj?.GetHashCode() ?? 0;

        public static SyntaxEqualityComparer<T> Default { get; } = new();
    }
}
