using HotChocolate.Data.Marten;

namespace HotChocolate.Data;

public static class MartenEnumerableExtensions
{
    /// <summary>
    /// Wraps a Marten query for asynchronous execution.
    /// </summary>
    /// <param name="source">The Marten query.</param>
    /// <typeparam name="T">The query result type.</typeparam>
    /// <returns>An executable that uses Marten's asynchronous query operations.</returns>
    public static IQueryableExecutable<T> AsMartenExecutable<T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new MartenQueryableExecutable<T>(source);
    }
}
