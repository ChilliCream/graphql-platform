using System.Collections.Generic;

namespace HotChocolate.Language
{
    internal static class ListExtensions
    {
        public static T Pop<T>(this IList<T> list)
        {
            int lastIndex = list.Count - 1;
            T p = list[lastIndex];
            list.RemoveAt(lastIndex);
            return p;
        }

        public static T Peek<T>(this IList<T> list)
        {
            int lastIndex = list.Count - 1;
            return list[lastIndex];
        }

        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }
    }
}
