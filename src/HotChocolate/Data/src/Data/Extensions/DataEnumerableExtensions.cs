using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data
{
    public static class DataEnumerableExtensions
    {
        public static QueryableExecutable<T> AsExecutable<T>(this IEnumerable<T> source)
        {
            return new QueryableExecutable<T>(source.AsQueryable());
        }

        public static QueryableExecutable<T> AsExecutable<T>(this IQueryable<T> source)
        {
            return new QueryableExecutable<T>(source);
        }
    }
}
