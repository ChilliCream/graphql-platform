namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of
/// `directive @remove(coordinate: _SchemaCoordinate) ON SCHEMA`.
/// </summary>
/// <param name="Coordinate">
/// A reference to the type system member that shall be removed.
/// </param>
internal sealed record RemoveDirective(SchemaCoordinate Coordinate);
