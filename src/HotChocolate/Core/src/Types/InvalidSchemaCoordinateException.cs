#nullable enable

namespace HotChocolate;

/// <summary>
/// This exception indicates that the specified
/// <see cref="InvalidSchemaCoordinateException.Coordinate"/>
/// could not be resolved.
/// </summary>
public class InvalidSchemaCoordinateException : Exception
{
    /// <summary>
    /// Creates new instance of <see cref="InvalidSchemaCoordinateException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="coordinate">The invalid schema coordinate.</param>
    public InvalidSchemaCoordinateException(string message, SchemaCoordinate coordinate)
        : base(message)
    {
        Coordinate = coordinate;
    }

    /// <summary>
    /// The invalid schema coordinate.
    /// </summary>
    /// <value></value>
    public SchemaCoordinate Coordinate { get; }
}
