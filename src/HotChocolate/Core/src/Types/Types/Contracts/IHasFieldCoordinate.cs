#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// GraphQL type system members that have a field coordinate.
/// </summary>
public interface IHasFieldCoordinate
{
    /// <summary>
    /// Field coordinate help with pointing to a field or argument in the schema.
    /// </summary>
    FieldCoordinate Coordinate { get; }
}
