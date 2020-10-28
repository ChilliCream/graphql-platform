using System.Collections.Generic;

namespace HotChocolate.Data
{
    public static class DataEnumerableExtensions
    {
        public static EnumerableExecutable<T> AsExecutable<T>(this IEnumerable<T> source)
        {
            return new EnumerableExecutable<T>(source);
        }
    }
}
