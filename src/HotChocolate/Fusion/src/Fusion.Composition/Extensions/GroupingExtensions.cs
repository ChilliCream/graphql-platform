namespace HotChocolate.Fusion.Extensions;

internal static class GroupingExtensions
{
    extension<TKey, TElement>(IGrouping<TKey, TElement> grouping)
    {
        public void Deconstruct(out TKey key, out IEnumerable<TElement> values)
        {
            key = grouping.Key;
            values = grouping;
        }
    }
}
