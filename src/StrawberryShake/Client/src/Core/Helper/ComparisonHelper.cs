using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StrawberryShake.Helper;

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

        var firstType = first.GetType();
        var secondType = second.GetType();

        if (firstType != secondType)
        {
            return false;
        }

        if (firstType == typeof(Dictionary<string, object?>) &&
            firstType == typeof(Dictionary<string, object?>))
        {
            var firstDict = Unsafe.As<Dictionary<string, object?>>(first);
            var secondDict = Unsafe.As<Dictionary<string, object?>>(second);

            return DictionaryEqualInternal(firstDict, secondDict);
        }

        if (firstType == typeof(List<object?>) &&
            firstType == typeof(List<object?>))
        {
            var firstDict = Unsafe.As<List<object?>>(first);
            var secondDict = Unsafe.As<List<object?>>(second);

            return ListEqualInternal(firstDict, secondDict);
        }

        if (typeof(IReadOnlyDictionary<string, object?>).IsAssignableFrom(firstType) &&
            typeof(IReadOnlyDictionary<string, object?>).IsAssignableFrom(secondType))
        {
            return DictionaryEqualInternal(
                (IReadOnlyDictionary<string, object?>)first,
                (IReadOnlyDictionary<string, object?>)second);
        }

        if (firstType != typeof(string) &&
            secondType != typeof(string) &&
            typeof(IEnumerable).IsAssignableFrom(firstType) &&
            typeof(IEnumerable).IsAssignableFrom(secondType))
        {
            return SequenceEqual((IEnumerable)first, (IEnumerable)second);
        }

        return Equals(first, second);
    }
}
