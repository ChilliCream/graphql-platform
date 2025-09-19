namespace StrawberryShake;

/// <summary>
/// Represents a query error.
/// </summary>
public interface IClientError
{
    /// <summary>
    /// Gets the error message.
    /// This property is mandatory and cannot be null.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets an error code that can be used to automatically
    /// process an error.
    /// This property is optional and can be null.
    /// </summary>
    string? Code { get; }

    /// <summary>
    /// Gets the path to the object that caused the error.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyList<object>? Path { get; }

    /// <summary>
    /// Gets the source text positions to which this error refers to.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyList<Location>? Locations { get; }

    /// <summary>
    /// Gets the exception associated with this error.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets non-spec error properties.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }
}
