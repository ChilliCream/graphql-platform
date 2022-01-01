using System;
using System.Runtime.Serialization;

#nullable enable

namespace HotChocolate;

/// <summary>
/// This exception indicates that the specified 
/// <see cref="InvalidSchemaCoordinateException.Coordinate"/> 
/// could not be resolved.
/// </summary>
[Serializable]
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
    /// Initializes a new instance of the <see cref="InvalidSchemaCoordinateException"/> 
    /// class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/>.</param>
    /// <param name="context">The <see cref="StreamingContext"/>.</param>
    protected InvalidSchemaCoordinateException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Coordinate = SchemaCoordinate.Parse(info.GetString(nameof(Coordinate))!);
    }

    /// <summary>
    /// The invalid schema coordinate.
    /// </summary>
    /// <value></value>
    public SchemaCoordinate Coordinate { get; }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(Coordinate), Coordinate.ToString());
    }
}
