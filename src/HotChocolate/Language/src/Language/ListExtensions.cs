using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language;

/// <summary>
/// Provides Stack and Queue extensions for <see cref="List{T}"/> to
/// the visitor APIs.
/// </summary>
public static class ListExtensions
{
    public static T Pop<T>(this IList<T> list)
    {
        var lastIndex = list.Count - 1;
        var p = list[lastIndex];
        list.RemoveAt(lastIndex);
        return p;
    }

    public static bool TryPop<T>(this IList<T> list, [MaybeNullWhen(false)] out T item)
    {
        if (list.Count > 0)
        {
            var lastIndex = list.Count - 1;
            item = list[lastIndex]!;
            list.RemoveAt(lastIndex);
            return true;
        }
        else
        {
            item = default!;
            return false;
        }
    }

    public static T Peek<T>(this IList<T> list)
    {
        var lastIndex = list.Count - 1;
        return list[lastIndex];
    }

    public static bool TryPeek<T>(this IList<T> list, [MaybeNullWhen(false)] out T item)
    {
        if (list.Count > 0)
        {
            var lastIndex = list.Count - 1;
            item = list[lastIndex]!;
            return true;
        }

        item = default;
        return false;
    }

    public static bool TryPeek<T>(
        this IList<T> list,
        int elements,
        [MaybeNullWhen(false)] out T item)
    {
        if (list.Count >= elements)
        {
            var lastIndex = list.Count - elements;
            item = list[lastIndex]!;
            return true;
        }

        item = default;
        return false;
    }

    public static T? PeekOrDefault<T>(this IList<T> list, T? defaultValue = default)
    {
        if (list.Count > 0)
        {
            var lastIndex = list.Count - 1;
            return list[lastIndex];
        }

        return defaultValue;
    }

    public static TSearch? PeekOrDefault<T, TSearch>(this IList<T> list, TSearch? defaultValue = default)
    {
        if (list.Count > 0)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is TSearch item)
                {
                    return item;
                }
            }
        }

        return defaultValue;
    }

    public static void Push<T>(this IList<T> list, T item)
    {
        list.Add(item);
    }
}
