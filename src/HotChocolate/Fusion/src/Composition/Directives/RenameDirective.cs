namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of
/// `directive @rename(coordinate: _SchemaCoordinate) ON SCHEMA`.
/// </summary>
/// <param name="Coordinate">
/// A reference to the type system member that shall be renamed.
/// </param>
/// <param name="NewName">
/// The new name that shall be applied to the type system member.
/// </param>
public sealed record RenameDirective(SchemaCoordinate Coordinate, string NewName);
