using System.Collections;

namespace StrawberryShake.Internal;

public static class ComparisonHelper
{
    public static bool SequenceEqual<TSource>(
        IEnumerable<TSource>? first,
        IEnumerable<TSource>? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
        {
            return false;
        }

        return first.SequenceEqual(second);
    }

    public static bool SequenceEqual<TSource>(
        IEnumerable<IEnumerable<TSource>?>? first,
        IEnumerable<IEnumerable<TSource>?>? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
        {
            return false;
        }

        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        while (e1.MoveNext())
        {
            if (!(e2.MoveNext() && SequenceEqual(e1.Current, e2.Current)))
            {
                return false;
            }
        }

        return !e2.MoveNext();
    }

    public static bool SequenceEqual(IEnumerable? first, IEnumerable? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
        {
            return false;
        }

        var e1 = first.GetEnumerator();
        var e2 = second.GetEnumerator();

        while (e1.MoveNext())
        {
            if (!e2.MoveNext())
            {
                return false;
            }

            if (!ObjEqual(e1.Current, e2.Current))
            {
                return false;
            }
        }

        return !e2.MoveNext();
    }

    public static bool DictionaryEqual(
        IReadOnlyDictionary<string, object?>? first,
        IReadOnlyDictionary<string, object?>? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
        {
            return false;
        }

        return DictionaryEqualInternal(second, first);
    }

    private static bool DictionaryEqualInternal<T>(T first, T second)
        where T : IReadOnlyDictionary<string, object?>
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var firstItem in first)
        {
            if (!second.TryGetValue(firstItem.Key, out var secondValue))
            {
                return false;
            }

            if (!ObjEqual(firstItem.Value, secondValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ListEqualInternal<T>(T first, T second)
        where T : IReadOnlyList<object?>
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            if (!ObjEqual(first[i], second[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ObjEqual(object? first, object? second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
        {
            return false;
        }

        if (first is Dictionary<string, object?> firstDict &&
            second is Dictionary<string, object?> secondDict)
        {
            return DictionaryEqualInternal(firstDict, secondDict);
        }

        if (first is List<object?> firstList &&
            second is List<object?> secondList)
        {
            return ListEqualInternal(firstList, secondList);
        }

        if (first is IReadOnlyDictionary<string, object?> firstReadDict &&
            second is IReadOnlyDictionary<string, object?> secondReadDict)
        {
            return DictionaryEqualInternal(firstReadDict, secondReadDict);
        }

        if (first is IEnumerable<KeyValuePair<string, object?>> firstKvp &&
            second is IEnumerable<KeyValuePair<string, object?>> secondKvp)
        {
            return DictionaryEqualInternal(
                firstKvp.ToDictionary(t => t.Key, t => t.Value),
                secondKvp.ToDictionary(t => t.Key, t => t.Value));
        }

        if (first is not string &&
            second is not string &&
            first is IEnumerable firstEnum &&
            second is IEnumerable secondEnum)
        {
            return SequenceEqual(firstEnum, secondEnum);
        }

        return Equals(first, second);
    }
}
