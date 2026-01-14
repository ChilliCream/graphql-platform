#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// A type system member that has a schema coordinate.
/// </summary>
public interface ISchemaCoordinateProvider
{
    /// <summary>
    /// Gets the schema coordinate of the type system member.
    /// </summary>
    SchemaCoordinate Coordinate { get; }
}
