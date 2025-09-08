namespace HotChocolate.Fusion.Extensions;

internal static class GroupingExtensions
{
    public static void Deconstruct<TKey, TElement>(
        this IGrouping<TKey, TElement> grouping,
        out TKey key,
        out IEnumerable<TElement> values)
    {
        key = grouping.Key;
        values = grouping;
    }
}
