using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Types
{
    internal static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> toAdd)
        {
            foreach (T add in toAdd)
            {
                list.Add(add);
            }
        }
    }
}
