
using System.Collections.Immutable;

namespace HotChocolate;

/// <summary>
/// Represents a GraphQL execution error.
/// </summary>
public sealed record Error : IError
{
    private string? _message;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message
    {
        get => _message ?? throw new InvalidOperationException("Message is required");
        init
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _message = value;
        }
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? Code => Extensions?.TryGetValue("code", out var value) is true ? value as string : null;

    /// <summary>
    /// Gets the path to the object that caused the error.
    /// </summary>
    public Path? Path { get; init; }

    /// <summary>
    /// Gets the source text positions to which this error refers to.
    /// </summary>
    public IReadOnlyList<Location>? Locations { get; init; }

    /// <summary>
    /// Gets the non-spec error properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }

    /// <summary>
    /// Gets the exception associated with this error.
    /// </summary>
    public Exception? Exception { get; init; }

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
    public IError WithCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            code = null;
        }

        if (Extensions is ImmutableDictionary<string, object?> d)
        {
            return code is null
                ? (this with { Extensions = d.Remove("code") })
                : (this with { Extensions = d.SetItem("code", code) });
        }

        if (Extensions is not null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object?>();
            builder.AddRange(Extensions);
            if (code is null)
            {
                builder.Remove("code");
            }
            else
            {
                builder.Add("code", code);
            }
            return this with { Extensions = builder.ToImmutable() };
        }

        if (code is not null)
        {
            return this with { Extensions = ImmutableDictionary<string, object?>.Empty.Add("code", code) };
        }

        return this;
    }

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="key" /> and <paramref name="value" />.
    /// </summary>
    /// <param name="key">The key of the custom error property.</param>
    /// <param name="value">The value of the custom error property.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="key" /> and <paramref name="value" />.
    /// </returns>
    public IError SetExtension(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (Extensions is ImmutableDictionary<string, object?> d)
        {
            return this with { Extensions = d.SetItem(key, value) };
        }

        if (Extensions is not null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object?>();
            builder.AddRange(Extensions!);
            builder.Add(key, value);
            return this with { Extensions = builder.ToImmutable() };
        }

        return this with { Extensions = ImmutableDictionary<string, object?>.Empty.Add(key, value) };
    }

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="key" /> removed.
    /// </summary>
    /// <param name="key">The key of the custom error property.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="key" /> removed.
    /// </returns>
    public IError RemoveExtension(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (Extensions is ImmutableDictionary<string, object?> d)
        {
            return this with { Extensions = d.Remove(key) };
        }

        if (Extensions is not null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object?>();
            builder.AddRange(Extensions!);
            builder.Remove(key);
            return this with { Extensions = builder.ToImmutable() };
        }

        return this;
    }

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="exception" />.
    /// </summary>
    /// <param name="exception">The exception associated with this error.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="exception" />.
    /// </returns>
    public IError WithException(Exception? exception)
        => this with { Exception = exception };

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="extensions" />.
    /// </summary>
    /// <param name="extensions">The extensions associated with this error.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="extensions" />.
    /// </returns>
    public IError WithExtensions(IReadOnlyDictionary<string, object?>? extensions)
        => this with { Extensions = extensions };

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="locations" />.
    /// </summary>
    /// <param name="locations">The locations associated with this error.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="locations" />.
    /// </returns>
    public IError WithLocations(IReadOnlyList<Location> locations)
        => this with { Locations = locations };

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="message" />.
    /// </summary>
    /// <param name="message">The message associated with this error.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="message" />.
    /// </returns>
    public IError   WithMessage(string message)
        => this with { Message = message };

    /// <summary>
    /// Creates a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </summary>
    /// <param name="path">The path associated with this error.</param>
    /// <returns>
    /// Returns a new error that contains all properties of this error
    /// but with the specified <paramref name="path" />.
    /// </returns>
    public IError WithPath(Path path)
        => this with { Path = path };
}
