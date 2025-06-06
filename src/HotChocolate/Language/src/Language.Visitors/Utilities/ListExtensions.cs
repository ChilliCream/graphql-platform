namespace HotChocolate.Language.Visitors;

internal static class ListExtensions
{
    public static T Pop<T>(this IList<T> list)
    {
        var lastIndex = list.Count - 1;
        var p = list[lastIndex];
        list.RemoveAt(lastIndex);
        return p;
    }

    public static T Peek<T>(this IList<T> list)
    {
        var lastIndex = list.Count - 1;
        return list[lastIndex];
    }

    public static bool TryPeek<T>(this IList<T> list, out T? item) where T : class
    {
        if (list.Count == 0)
        {
            item = null;
            return false;
        }

        item = Peek(list);
        return true;
    }

    public static void Push<T>(this IList<T> list, T item)
    {
        list.Add(item);
    }
}
