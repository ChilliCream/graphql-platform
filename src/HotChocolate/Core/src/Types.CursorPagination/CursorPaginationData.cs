using System.Collections.Immutable;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The raw pagination data that is returned from the query executor.
/// </summary>
/// <param name="edges">
/// The edges that belong to this connection.
/// </param>
/// <param name="totalCount">
/// The total count of items of this connection.
/// </param>
/// <typeparam name="TEntity">
/// The entity type of the data.
/// </typeparam>
public readonly struct CursorPaginationData<TEntity>(
    ImmutableArray<Edge<TEntity>> edges,
    int? totalCount)
{
    /// <summary>
    /// Gets the edges that belong to this connection.
    /// </summary>
    public ImmutableArray<Edge<TEntity>> Edges { get; } = edges;

    /// <summary>
    /// Gets the total count of items of this connection.
    /// </summary>
    public int? TotalCount { get; } = totalCount;
}
