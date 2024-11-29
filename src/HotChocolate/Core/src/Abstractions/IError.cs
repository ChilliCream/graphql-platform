namespace HotChocolate;

/// <summary>
/// Represents a GraphQL execution error.
/// </summary>
public interface IError
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
    Path? Path { get; }

    /// <summary>
    /// Gets the source text positions to which this error refers to.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyList<Location>? Locations { get; }

    /// <summary>
    /// Gets non-spec error properties.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Extensions { get; }

    /// <summary>
    /// Gets the exception associated with this error.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="message" />.
    /// </summary>
    /// <param name="message">
    /// The error message.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="message" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="message" /> is null or empty.
    /// </exception>
    IError WithMessage(string message);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="code" />.
    /// </summary>
    /// <param name="code">
    /// An error code that is specified as custom error property.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="code" />.
    /// </returns>
    IError WithCode(string? code);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with <see cref="Code"/> removed.
    /// </summary>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with <see cref="Code"/> removed.
    /// </returns>
    IError RemoveCode();

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </summary>
    /// <param name="path">
    /// A path representing a certain syntax node of a query or schema.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </returns>
    IError WithPath(Path? path);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </summary>
    /// <param name="path">
    /// A path representing a certain syntax node of a query or schema.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </returns>
    IError WithPath(IReadOnlyList<object>? path);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the <see cref="Path"/> removed.
    /// </summary>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the <see cref="Path"/> removed.
    /// </returns>
    IError RemovePath();

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="locations" />.
    /// </summary>
    /// <param name="locations">
    /// A collection of locations referring to certain
    /// syntax nodes of a query or schema.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="locations" />.
    /// </returns>
    IError WithLocations(IReadOnlyList<Location>? locations);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the <see cref="Locations"/> removed.
    /// </summary>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the <see cref="Locations"/> removed.
    /// </returns>
    IError RemoveLocations();

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="extensions" />.
    /// </summary>
    /// <param name="extensions">
    /// A collection of custom error properties.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="extensions" />.
    /// </returns>
    IError WithExtensions(IReadOnlyDictionary<string, object?> extensions);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the <see cref="Extensions"/> removed.
    /// </summary>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the <see cref="Extensions"/> removed.
    /// </returns>
    IError RemoveExtensions();

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with and additional custom error property.
    /// </summary>
    /// <param name="key">The custom error property name.</param>
    /// <param name="value">The value of the custom error property.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with and additional custom error property.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="key" /> is null or empty.
    /// </exception>
    IError SetExtension(string key, object? value);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified additional custom error property removed.
    /// </summary>
    /// <param name="key">The custom error property name.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified additional custom error property removed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="key" /> is null or empty.
    /// </exception>
    IError RemoveExtension(string key);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="exception" />.
    /// </summary>
    /// <param name="exception">
    /// The .net exception that caused this error.
    /// </param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="exception" />.
    /// </returns>
    IError WithException(Exception? exception);

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but removed the exception from it.
    /// </summary>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but without any exception details.
    /// </returns>
    IError RemoveException();
}
