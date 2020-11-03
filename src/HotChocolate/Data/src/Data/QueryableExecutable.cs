using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.Data.ErrorHelper;

namespace HotChocolate.Data
{
    public class QueryableExecutable<T> : IExecutable<T>
    {
        public QueryableExecutable(IQueryable<T> queryable)
        {
            Source = queryable;
            InMemory = Source is EnumerableQuery;
        }

        /// <summary>
        /// The current state of the executable
        /// </summary>
        public IQueryable<T> Source { get; }

        object IExecutable.Source => Source;

        /// <summary>
        /// Is true if <see cref="QueryableExecutable{T}.Source"/> source is a in memory query and
        /// false if it is a database query
        /// </summary>
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

        public virtual async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            if (Source is IAsyncEnumerable<T> ae)
            {
                var result = new List<T>();
                await foreach (T element in ae.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    result.Add(element);
                }

                return result;
            }

            return Source.ToList();
        }

        public virtual async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken)
        {
            if (Source is IAsyncEnumerable<T> ae)
            {
                await using IAsyncEnumerator<T> enumerator =
                    ae.GetAsyncEnumerator(cancellationToken);

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    return enumerator.Current;
                }

                return default(T)!;
            }

            return Source.FirstOrDefault();
        }

        public virtual async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken)
        {
            if (Source is IAsyncEnumerable<T> ae)
            {
                await using IAsyncEnumerator<T> enumerator =
                    ae.GetAsyncEnumerator(cancellationToken);

                object? result;
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    result = enumerator.Current;
                }
                else
                {
                    result = default(T)!;
                }

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    result = ProjectionProvider_CreateMoreThanOneError();
                }

                return result;
            }

            try
            {
                return Source.SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                return ProjectionProvider_CreateMoreThanOneError();
            }
        }

        public virtual string Print()
        {
            return Source.ToString() ?? "";
        }
    }
}
