namespace HotChocolate.Fusion.Execution.Introspection;

/// <summary>
/// Represents the backing data for a <c>__SearchResult</c> introspection type instance.
/// </summary>
internal sealed class SearchResultData
{
    /// <summary>
    /// Gets the schema coordinate of the matched element.
    /// </summary>
    public required SchemaCoordinate Coordinate { get; init; }

    /// <summary>
    /// Gets the resolved type system definition (e.g. <see cref="HotChocolate.Types.ITypeDefinition"/>,
    /// <see cref="HotChocolate.Types.IOutputFieldDefinition"/>,
    /// <see cref="HotChocolate.Types.IInputValueDefinition"/>,
    /// <see cref="HotChocolate.Types.IEnumValue"/>,
    /// or <see cref="HotChocolate.Types.IDirectiveDefinition"/>).
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
