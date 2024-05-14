using System.Collections;
using static HotChocolate.Data.ErrorHelper;

namespace HotChocolate.Data;

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
    public virtual QueryableExecutable<T> WithSource(IQueryable<T> source)
    {
        return new QueryableExecutable<T>(source);
    }

    public virtual async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
    {
        if (Source is not IAsyncEnumerable<T> ae)
        {
            return Source.ToList();
        }

        var result = new List<T>();

        await foreach (var element in ae.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(element);
        }

        return result;
    }

    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    public virtual async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        if (Source is IAsyncEnumerable<T> ae)
        {
            await using var enumerator =
                ae.GetAsyncEnumerator(cancellationToken);

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }

            return default!;
        }

        return Source.FirstOrDefault();
    }

    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);

    public virtual async ValueTask<T?> SingleOrDefaultAsync(
        CancellationToken cancellationToken)
    {
        if (Source is IAsyncEnumerable<T> ae)
        {
            await using var enumerator = ae.GetAsyncEnumerator(cancellationToken);

            T? result;
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                result = enumerator.Current;
            }
            else
            {
                result = default;
            }

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                throw new GraphQLException( ProjectionProvider_CreateMoreThanOneError());
            }

            return result;
        }

        try
        {
            return Source.SingleOrDefault();
        }
        catch (InvalidOperationException)
        {
            throw new GraphQLException(ProjectionProvider_CreateMoreThanOneError());
        }
    }

    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);

    public virtual string Print()
    {
        return Source.ToString() ?? "";
    }
}
