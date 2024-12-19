using System.Collections.Immutable;

namespace HotChocolate.Fusion.Extensions;

internal static class ImmutableArrayExtensions
{
    public static int Count<T>(this ImmutableArray<T> array, Predicate<T> predicate)
    {
        var count = 0;

        foreach (var item in array)
        {
            if (predicate(item))
            {
                count++;
            }
        }

        return count;
    }
}
