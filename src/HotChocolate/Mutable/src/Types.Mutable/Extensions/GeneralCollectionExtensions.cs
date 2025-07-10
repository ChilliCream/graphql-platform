namespace HotChocolate.Types.Mutable;

internal static class GeneralCollectionExtensions
{
    public static OrderedDictionary<TKey, TElement> ToOrderedDictionary<TElement, TKey>(
        this IEnumerable<TElement> values,
        Func<TElement, TKey> keySelector)
        where TKey : IEquatable<TKey>
        => values.ToOrderedDictionary(keySelector, x => x);

    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TElement, TKey, TValue>(
        this IEnumerable<TElement> values,
        Func<TElement, TKey> keySelector,
        Func<TElement, TValue> valueSelector)
        where TKey : IEquatable<TKey>
    {
        ArgumentNullException.ThrowIfNull(values);

        var dictionary = new OrderedDictionary<TKey, TValue>();

        foreach (var item in values)
        {
            dictionary.Add(keySelector(item), valueSelector(item));
        }

        return dictionary;
    }

    public static IEnumerable<T> OrderBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        bool ifTrue)
        where TKey : IComparable<TKey>
        => ifTrue ? source.OrderBy(keySelector) : source;
}
