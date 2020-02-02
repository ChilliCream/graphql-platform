using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
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

        public static bool TryPeek<T>(this IList<T> list, out T? item) where T : class
        {
            if (list.Count == 0)
            {
                item = default;
                return false;
            }

            item = Peek<T>(list);
            return true;
        }

        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }
    }
}
