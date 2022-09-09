using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.Helper;

public static class ComparisonHelper
{
    public static bool SequenceEqual<TSource>(
        IEnumerable<TSource>? first,
        IEnumerable<TSource>? second)
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (second is not null && first is not null)
        {
            return first.SequenceEqual(second);
        }

        return false;
    }

    public static bool SequenceEqual<TSource>(
        IEnumerable<IEnumerable<TSource>?>? first,
        IEnumerable<IEnumerable<TSource>?>? second)
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is not null && second is not null)
        {
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

        return false;
    }

    public static bool SequenceEqual(
        IEnumerable? first,
        IEnumerable? second)
    {
        if (first is null && second is null)
        {
            return true;
        }

        if (first is not null && second is not null)
        {
            var e1 = first.GetEnumerator();
            var e2 = second.GetEnumerator();

            while (e1.MoveNext())
            {
                if (!e2.MoveNext())
                {
                    return false;
                }

                if (e1.Current is null && e2.Current is not null)
                {
                    return false;
                }

                if (e1.Current is not null && e2.Current is null)
                {
                    return false;
                }

                if (e1.Current is IEnumerable i1 &&
                    e2.Current is IEnumerable i2 &&
                    !SequenceEqual(i1, i2))
                {
                    return false;
                }

                if (e1.Current is not null &&
                    e2.Current is not null &&
                    !e1.Current.Equals(e2.Current))
                {
                    return false;
                }
            }

            return !e2.MoveNext();
        }

        return false;
    }

    public static bool DictionaryEqual(
        IReadOnlyDictionary<string, object?>? first,
        IReadOnlyDictionary<string, object?>? second)
    {
        // the variables dictionary is the same or both are null.
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if ((first == null) || (second == null))
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var key in first.Keys)
        {
            if (!first.TryGetValue(key, out var a) ||
                !second.TryGetValue(key, out var b))
            {
                return false;
            }

            if (a is IEnumerable<KeyValuePair<string, object?>> k1 &&
                b is IEnumerable<KeyValuePair<string, object?>> k2)
            {
                if (!DictionaryEqual(k1.ToDictionary(x => x.Key, x => x.Value), k2.ToDictionary(x => x.Key, x => x.Value)))
                {
                    return false;
                }
            }
            else if (a is IEnumerable e1 &&
                     b is IEnumerable e2)
            {
                // Check the contents of the collection, assuming order is important
                if (!SequenceEqual(e1, e2))
                {
                    return false;
                }
            }
            else if (!Equals(a, b))
            {
                return false;
            }
        }

        return true;
    }
}
