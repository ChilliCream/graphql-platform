#if NET7_0_OR_GREATER
namespace GreenDonut;

/// <summary>
/// The key of a cached task.
/// </summary>
public readonly record struct TaskCacheKey(string Type, object Key);
#else
using System;

namespace GreenDonut;

/// <summary>
/// The key of a cached task.
/// </summary>
public readonly struct TaskCacheKey : IEquatable<TaskCacheKey>
{
    /// <summary>
    /// Creates a new instance of <see cref="TaskCacheKey"/>.
    /// </summary>
    /// <param name="type">
    /// The key type.
    /// </param>
    /// <param name="key">
    /// The entity key.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> or <paramref name="key"/> is <c>null</c>.
    /// </exception>
    public TaskCacheKey(string type, object key)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// Gets the key type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the entity key.
    /// </summary>
    public object Key { get; }

    /// <inheritdoc />
    public bool Equals(TaskCacheKey other)
        => Type.Equals(other.Type, StringComparison.Ordinal) &&
            Key.Equals(other.Key);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is TaskCacheKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (Type.GetHashCode() * 397) ^ Key.GetHashCode();
        }
    }
}
#endif