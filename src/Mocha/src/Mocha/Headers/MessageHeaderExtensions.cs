using System.Diagnostics.CodeAnalysis;

namespace Mocha;

/// <summary>
/// Provides extension methods for reading and writing message headers using strongly-typed <see cref="ContextDataKey{T}"/> keys.
/// </summary>
public static class MessageHeaderExtensions
{
    /// <summary>
    /// Copies all headers from the source collection to the target collection.
    /// </summary>
    /// <param name="headers">The source headers to copy from.</param>
    /// <param name="target">The target headers to copy into.</param>
    /// <returns>The target headers collection for method chaining.</returns>
    public static IHeaders CopyTo(this IReadOnlyHeaders headers, IHeaders target)
    {
        foreach (var header in headers)
        {
            target.Set(header.Key, header.Value);
        }
        return target;
    }

    /// <summary>
    /// Copies a single header identified by a typed key from the source to the target, if it exists.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The source headers to copy from.</param>
    /// <param name="target">The target headers to copy into.</param>
    /// <param name="key">The typed key identifying the header to copy.</param>
    /// <returns>The target headers collection for method chaining.</returns>
    internal static IHeaders CopyTo<T>(this IReadOnlyHeaders headers, IHeaders target, ContextDataKey<T> key)
    {
        if (headers.TryGet(key, out var value))
        {
            target.Set(key.Key, value);
        }
        return target;
    }

    /// <summary>
    /// Sets a header value using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The headers collection.</param>
    /// <param name="key">The typed key for the header.</param>
    /// <param name="value">The value to set.</param>
    internal static void Set<T>(this IHeaders headers, ContextDataKey<T> key, T value)
    {
        headers.Set(key.Key, value);
    }

    /// <summary>
    /// Attempts to add a header value using a strongly-typed key, only if the key does not already exist.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The headers collection.</param>
    /// <param name="key">The typed key for the header.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><c>true</c> if the value was added; <c>false</c> if the key already exists.</returns>
    internal static bool TryAdd<T>(this IHeaders headers, ContextDataKey<T> key, T value)
    {
        if (headers.ContainsKey(key.Key))
        {
            return false;
        }

        headers.Set(key.Key, value);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve a header value using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The headers collection.</param>
    /// <param name="key">The typed key for the header.</param>
    /// <param name="value">When this method returns, contains the typed value if found and of the correct type.</param>
    /// <returns><c>true</c> if the key was found and the value is of type <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    internal static bool TryGet<T>(this IReadOnlyHeaders headers, ContextDataKey<T> key, [NotNullWhen(true)] out T value)
    {
        if (headers.TryGetValue(key.Key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets a header value using a strongly-typed key, returning the default value if not found or not of the expected type.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The headers collection.</param>
    /// <param name="key">The typed key for the header.</param>
    /// <returns>The typed value if found; otherwise, the default value of <typeparamref name="T"/>.</returns>
    internal static T Get<T>(this IReadOnlyHeaders headers, ContextDataKey<T> key)
    {
        if (headers.TryGetValue(key.Key, out var objValue) && objValue is T typedValue)
        {
            return typedValue;
        }

        return default!;
    }

    /// <summary>
    /// Sets a value in a dictionary using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The dictionary to set the value in.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <param name="value">The value to set.</param>
    internal static void Set<T>(this IDictionary<string, object?> headers, ContextDataKey<T> header, T value)
    {
        headers[header.Key] = value;
    }

    /// <summary>
    /// Attempts to add a value to a dictionary using a strongly-typed key, only if the key does not already exist.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The dictionary to add the value to.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <param name="value">The value to add.</param>
    /// <returns><c>true</c> if the value was added; <c>false</c> if the key already exists.</returns>
    internal static bool TryAdd<T>(this IDictionary<string, object?> headers, ContextDataKey<T> header, T value)
    {
        return headers.TryAdd(header.Key, value);
    }

    /// <summary>
    /// Gets a value from a dictionary using a strongly-typed key, returning the default if not found.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The dictionary to retrieve from.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <returns>The typed value if found; otherwise, the default value of <typeparamref name="T"/>.</returns>
    internal static T Get<T>(this IDictionary<string, object?> headers, ContextDataKey<T> header)
    {
        if (headers.TryGetValue(header.Key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default!;
    }

    /// <summary>
    /// Attempts to retrieve a value from a dictionary using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The dictionary to retrieve from.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <param name="value">When this method returns, contains the typed value if found and of the correct type.</param>
    /// <returns><c>true</c> if the key was found and the value is of type <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    internal static bool TryGet<T>(
        this IDictionary<string, object?> headers,
        ContextDataKey<T> header,
        [NotNullWhen(true)] out T value)
    {
        if (headers.TryGetValue(header.Key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets a value from a read-only dictionary using a strongly-typed key, returning the default if not found.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The read-only dictionary to retrieve from.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <returns>The typed value if found; otherwise, the default value of <typeparamref name="T"/>.</returns>
    internal static T Get<T>(this IReadOnlyDictionary<string, object?> headers, ContextDataKey<T> header)
    {
        if (headers.TryGetValue(header.Key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default!;
    }

    /// <summary>
    /// Attempts to retrieve a value from a read-only dictionary using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The read-only dictionary to retrieve from.</param>
    /// <param name="header">The typed key for the header.</param>
    /// <param name="value">When this method returns, contains the typed value if found and of the correct type.</param>
    /// <returns><c>true</c> if the key was found and the value is of type <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    internal static bool TryGet<T>(
        this IReadOnlyDictionary<string, object?> headers,
        ContextDataKey<T> header,
        [NotNullWhen(true)] out T value)
    {
        if (headers.TryGetValue(header.Key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Copies a single header value from a read-only dictionary to a mutable dictionary using a strongly-typed key.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="headers">The source read-only dictionary.</param>
    /// <param name="target">The target mutable dictionary.</param>
    /// <param name="header">The typed key identifying the header to copy.</param>
    /// <returns><c>true</c> if the header was found and copied; otherwise, <c>false</c>.</returns>
    internal static bool CopyTo<T>(
        this IReadOnlyDictionary<string, object?> headers,
        IDictionary<string, object?> target,
        ContextDataKey<T> header)
    {
        if (headers.TryGet(header, out var value))
        {
            target[header.Key] = value;
            return true;
        }

        return false;
    }
}
