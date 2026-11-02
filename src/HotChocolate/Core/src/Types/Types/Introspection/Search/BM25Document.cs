namespace HotChocolate.Types.Introspection;

/// <summary>
/// Represents a single document in the BM25 search index,
/// mapping a schema coordinate to its searchable text content.
/// </summary>
internal readonly record struct BM25Document(
    SchemaCoordinate Coordinate,
    string Text);
