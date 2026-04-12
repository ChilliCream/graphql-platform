namespace HotChocolate;

/// <summary>
/// Provides semantic search capabilities over a GraphQL schema,
/// allowing consumers to find schema elements by natural language queries
/// and to discover paths from a given schema coordinate back to a root type.
/// </summary>
public interface ISchemaSearchProvider
{
    /// <summary>
    /// Searches the schema for elements matching the specified query.
    /// </summary>
    /// <param name="query">
    /// The search query string.
    /// </param>
    /// <param name="first">
    /// The maximum number of results to return.
    /// </param>
    /// <param name="after">
    /// An opaque cursor for forward pagination.
    /// Pass <c>null</c> to start from the beginning.
    /// </param>
    /// <param name="minScore">
    /// The minimum relevance score in the range [0.0, 1.0].
    /// Results with a score below this threshold are excluded.
    /// Pass <c>null</c> to include all results.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A list of <see cref="SchemaSearchResult"/> ordered by relevance.
    /// </returns>
    ValueTask<IReadOnlyList<SchemaSearchResult>> SearchAsync(
        string query,
        int first,
        string? after,
        float? minScore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the paths from the specified schema coordinate to a root type.
    /// </summary>
    /// <param name="coordinate">
    /// The schema coordinate from which to trace paths to a root type.
    /// </param>
    /// <param name="maxPaths">
    /// The maximum number of paths to return.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A list of <see cref="SchemaCoordinatePath"/> instances,
    /// each representing an ordered path from the coordinate to a root type.
    /// </returns>
    ValueTask<IReadOnlyList<SchemaCoordinatePath>> GetPathsToRootAsync(
        SchemaCoordinate coordinate,
        int maxPaths,
        CancellationToken cancellationToken = default);
}
