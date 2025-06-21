using HotChocolate.Collections.Immutable;

namespace HotChocolate;

/// <summary>
/// A builder for creating a GraphQL execution error.
/// </summary>
public sealed class ErrorBuilder
{
    private string? _message;
    private Path? _path;
    private Exception? _exception;
    private ImmutableOrderedDictionary<string, object?>.Builder? _extensions;
    private List<Location>? _locations;

    private ErrorBuilder() { }

    /// <summary>
    /// Sets the message of the error.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <returns>The error builder.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="message" /> is <c>null</c> or empty.
    /// </exception>
    public ErrorBuilder SetMessage(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        _message = message;
        return this;
    }

    /// <summary>
    /// Sets the code of the error.
    /// </summary>
    /// <param name="code">The code of the error.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder SetCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            code = null;
        }

        _extensions ??= ImmutableOrderedDictionary.CreateBuilder<string, object?>();

        if(code is null)
        {
            _extensions.Remove(nameof(code));
            return this;
        }

        if (_extensions.ContainsKey(nameof(code)))
        {
            _extensions[nameof(code)] = code;
        }
        else
        {
            _extensions.Insert(0, nameof(code), code);
        }

        return this;
    }

    /// <summary>
    /// Sets the path of the error.
    /// </summary>
    /// <param name="path">The path of the error.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder SetPath(Path? path)
    {
        _path = path;
        return this;
    }

    /// <summary>
    /// Adds a GraphQL operation document location to the error.
    /// </summary>
    /// <param name="location">The location of the error.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder AddLocation(Location location)
    {
        _locations ??= [];
        _locations.Add(location);
        return this;
    }

    /// <summary>
    /// Clears the locations of the error.
    /// </summary>
    /// <returns>The error builder.</returns>
    public ErrorBuilder ClearLocations()
    {
        _locations = null;
        return this;
    }

    /// <summary>
    /// Sets the exception of the error.
    /// </summary>
    /// <param name="exception">The exception of the error.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder SetException(Exception? exception)
    {
        _exception = exception;
        return this;
    }

    /// <summary>
    /// Sets an extension of the error.
    /// </summary>
    /// <param name="key">The key of the extension.</param>
    /// <param name="value">The value of the extension.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder SetExtension(string key, object? value)
    {
        _extensions ??= ImmutableOrderedDictionary.CreateBuilder<string, object?>();
        _extensions[key] = value;
        return this;
    }

    /// <summary>
    /// Removes an extension of the error.
    /// </summary>
    /// <param name="key">The key of the extension.</param>
    /// <returns>The error builder.</returns>
    public ErrorBuilder RemoveExtension(string key)
    {
        _extensions ??= ImmutableOrderedDictionary.CreateBuilder<string, object?>();
        _extensions.Remove(key);
        return this;
    }

    /// <summary>
    /// Clears the extensions of the error.
    /// </summary>
    /// <returns>The error builder.</returns>
    public ErrorBuilder ClearExtensions()
    {
        _extensions = null;
        return this;
    }

    /// <summary>
    /// Builds the error.
    /// </summary>
    /// <returns>The error.</returns>
    public IError Build()
    {
        if (_message is null)
        {
            throw new InvalidOperationException("Message is required");
        }

        return new Error
        {
            Message = _message,
            Path = _path,
            Exception = _exception,
            Extensions = _extensions?.ToImmutable(),
            Locations = _locations
        };
    }

    /// <summary>
    /// Creates a new error builder.
    /// </summary>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder New() => new();

    /// <summary>
    /// Creates a new error builder from an error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder FromError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        ImmutableOrderedDictionary<string, object?>.Builder? extensions = null;
        List<Location>? locations = null;

        if (error.Extensions is not null)
        {
            extensions = ImmutableOrderedDictionary.CreateBuilder<string, object?>();
            extensions.AddRange(error.Extensions);
        }

        if (error.Locations is not null)
        {
            locations = [];
            locations.AddRange(error.Locations);
        }

        return new ErrorBuilder
        {
            _message = error.Message,
            _path = error.Path,
            _exception = error.Exception,
            _extensions = extensions,
            _locations = locations
        };
    }

    public static ErrorBuilder FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return New()
            .SetMessage("Unexpected Execution Error")
            .SetException(exception);
    }
}
