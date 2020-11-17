using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data
{
    public static class DataEnumerableExtensions
    {
        /// <summary>
        /// Wraps the <see cref="IEnumerable{T}"/> with <see cref="QueryableExecutable{T}"/> to help
        /// the execution engine to execute it more efficient
        /// </summary>
        /// <param name="source">The source of the <see cref="IExecutable"/></param>
        /// <typeparam name="T">The type parameter</typeparam>
        /// <returns>The wrapped object</returns>
        public static QueryableExecutable<T> AsExecutable<T>(this IEnumerable<T> source)
        {
            return new QueryableExecutable<T>(source.AsQueryable());
        }

        /// <summary>
        /// Wraps the <see cref="IQueryable"/> with <see cref="QueryableExecutable{T}"/> to help the
        /// execution engine to execute it more efficient
        /// </summary>
        /// <param name="source">The source of the <see cref="IExecutable"/></param>
        /// <typeparam name="T">The type parameter</typeparam>
        /// <returns>The wrapped object</returns>
        public static QueryableExecutable<T> AsExecutable<T>(this IQueryable<T> source)
        {
            return new QueryableExecutable<T>(source);
        }
    }
}
