namespace HotChocolate.Types.Introspection;

/// <summary>
/// Represents the backing data for a <c>__SearchResult</c> introspection type instance.
/// </summary>
internal sealed class SearchResultInfo
{
    /// <summary>
    /// Gets the schema coordinate of the matched element.
    /// </summary>
    public required SchemaCoordinate Coordinate { get; init; }

    /// <summary>
    /// Gets the resolved type system definition (e.g. <see cref="IType"/>,
    /// <see cref="IOutputFieldDefinition"/>, <see cref="IInputValueDefinition"/>,
    /// <see cref="IEnumValue"/>, or <see cref="IDirectiveDefinition"/>).
    /// </summary>
    public required object Definition { get; init; }

    /// <summary>
    /// Gets the paths from the matched element to a root type,
    /// each serialized as a string of schema coordinates.
    /// </summary>
    public required IReadOnlyList<string> PathsToRoot { get; init; }

    /// <summary>
    /// Gets the relevance score of the match, or <c>null</c> if scoring is not supported.
    /// </summary>
    public float? Score { get; init; }

    /// <summary>
    /// Gets the opaque cursor for pagination.
    /// </summary>
    public required string Cursor { get; init; }
}
