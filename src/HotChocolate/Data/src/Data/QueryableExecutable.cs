using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Data
{
    public class QueryableExecutable<T> : IQueryableExecutable<T>
    {
        public QueryableExecutable(IQueryable<T> queryable)
        {
            Source = queryable;
            InMemory = Source is EnumerableQuery;
        }

        public IQueryable<T> Source { get; }

        public bool InMemory { get; }

        /// <summary>
        /// Returns a new enumerable executable with the provided source
        /// </summary>
        /// <param name="source">The source that should be set</param>
        /// <returns>The new instance of an enumerable executable</returns>
        public QueryableExecutable<T> WithSource(IQueryable<T> source)
        {
            return new QueryableExecutable<T>(source);
        }

        public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<IList>(Source.ToList());
        }

        public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<object?>(Source.FirstOrDefault());
        }

        public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<object?>(Source.SingleOrDefault());
        }

        public string Print()
        {
            return Source.ToString() ?? "";
        }
    }
}
