namespace HotChocolate.Skimmed;

internal static class GeneralCollectionExtensions
{
    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TElement, TKey, TValue>(
        this IEnumerable<TElement> values,
        Func<TElement, TKey> keySelector,
        Func<TElement, TValue> valueSelector)
        where TKey : IEquatable<TKey>
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var dictionary = new OrderedDictionary<TKey, TValue>();

        foreach (var item in values)
        {
            dictionary.Add(keySelector(item), valueSelector(item));
        }

        return dictionary;
    }
}
