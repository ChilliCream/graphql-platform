namespace HotChocolate.Configuration;

internal static class ListExtensions
{
    public static void TryAdd<T>(this List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }
}
