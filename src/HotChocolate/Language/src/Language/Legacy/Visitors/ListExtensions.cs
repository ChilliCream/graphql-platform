using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language
{
    public static class ListExtensions
    {
        public static T Pop<T>(this IList<T> list)
        {
            var lastIndex = list.Count - 1;
            T p = list[lastIndex];
            list.RemoveAt(lastIndex);
            return p;
        }

        public static bool TryPop<T>(this IList<T> list, [NotNullWhen(true)]out T item)
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

        public static bool TryPeek<T>(this IList<T> list, [NotNullWhen(true)]out T item)
        {
            if (list.Count > 0)
            {
                var lastIndex = list.Count - 1;
                item = list[lastIndex]!;
                return true;
            }

            item = default!;
            return false;
        }

        public static T PeekOrDefault<T>(this IList<T> list, T defaultValue = default!)
        {
            if (list.Count > 0)
            {
                var lastIndex = list.Count - 1;
                return list[lastIndex];
            }

            return defaultValue;
        }

        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }
    }
}
