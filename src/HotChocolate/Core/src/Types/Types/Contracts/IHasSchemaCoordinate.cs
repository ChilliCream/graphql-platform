#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that have a schema coordinate.
/// </summary>
public interface IHasSchemaCoordinate
{
    /// <summary>
    /// Schema coordinate help with pointing to a field or argument in the schema.
    /// </summary>
    SchemaCoordinate Coordinate { get; }
}
