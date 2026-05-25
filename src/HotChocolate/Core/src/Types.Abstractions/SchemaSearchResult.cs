namespace HotChocolate;

/// <summary>
/// Represents a single result from a schema search operation.
/// </summary>
public readonly record struct SchemaSearchResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaSearchResult"/>.
    /// </summary>
    /// <param name="coordinate">
    /// The schema coordinate of the matched element.
    /// </param>
    /// <param name="score">
    /// The relevance score of the match in the range [0.0, 1.0],
    /// or <c>null</c> if scoring is not supported.
    /// </param>
    /// <param name="cursor">
    /// An opaque cursor that can be used for pagination.
    /// </param>
    public SchemaSearchResult(
        SchemaCoordinate coordinate,
        float? score,
        string cursor)
    {
        Coordinate = coordinate;
        Score = score;
        Cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
    }

    /// <summary>
    /// Gets the schema coordinate of the matched element.
    /// </summary>
    public SchemaCoordinate Coordinate { get; }

    /// <summary>
    /// Gets the relevance score of the match in the range [0.0, 1.0],
    /// or <c>null</c> if scoring is not supported by the provider.
    /// </summary>
    public float? Score { get; }

    /// <summary>
    /// Gets the opaque cursor that can be used for pagination.
    /// </summary>
    public string Cursor { get; }
}
