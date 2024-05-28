using System;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate;

/// <summary>
/// The optional type is used to differentiate between not set and set input values.
/// </summary>
public readonly struct Optional<T>
    : IEquatable<Optional<T>>
    , IOptional
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Optional{T}"/> struct.
    /// </summary>
    /// <param name="value">The actual value.</param>
    public Optional(T? value)
    {
        Value = value;
        HasValue = true;
    }

    private Optional(T? value, bool hasValue)
    {
        Value = value;
        HasValue = hasValue;
    }

    /// <summary>
    /// The name value.
    /// </summary>
    public T? Value { get; }

    object? IOptional.Value => Value;

    /// <summary>
    /// <c>true</c> if the optional was explicitly set.
    /// </summary>
    #if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(Value))]
    #endif
    public bool HasValue { get; }

    /// <summary>
    /// <c>true</c> if the optional was not explicitly set.
    /// </summary>
    public bool IsEmpty => !HasValue;

    /// <summary>
    /// Provides the name string.
    /// </summary>
    /// <returns>The name string value</returns>
    public override string ToString()
    {
        return HasValue ? Value?.ToString() ?? "null" : "unspecified";
    }

    /// <summary>
    /// Compares this <see cref="Optional{T}"/> value to another value.
    /// </summary>
    /// <param name="other">
    /// The second <see cref="Optional{T}"/> for comparison.
    /// </param>
    /// <returns>
    /// <c>true</c> if both <see cref="Optional{T}"/> values are equal.
    /// </returns>
    public bool Equals(Optional<T> other)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }

        if (HasValue != other.HasValue)
        {
            return false;
        }

        return Equals(Value, other.Value);
    }

    /// <summary>
    /// Compares this <see cref="Optional{T}"/> value to another value.
    /// </summary>
    /// <param name="obj">
    /// The second <see cref="Optional{T}"/> for comparison.
    /// </param>
    /// <returns>
    /// <c>true</c> if both <see cref="Optional{T}"/> values are equal.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return IsEmpty;
        }
        return obj is Optional<T> n && Equals(n);
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="Optional{T}"/> object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in hashing
    /// algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        return (HasValue ? Value?.GetHashCode() ?? 0 : 0);
    }

    /// <summary>
    /// Operator call through to Equals
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if both <see cref="Optional{T}"/> values are equal.
    /// </returns>
    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Operator call through to Equals
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if both <see cref="Optional{T}"/> values are not equal.
    /// </returns>
    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Implicitly creates a new <see cref="Optional{T}"/> from
    /// the given value.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator Optional<T>(T value)
        => new(value);

    /// <summary>
    /// Implicitly gets the optional value.
    /// </summary>
    [return: MaybeNull]
    public static implicit operator T(Optional<T> optional)
        => optional.Value;

    /// <summary>
    /// Creates an empty optional that provides a default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    public static Optional<T> Empty(T? defaultValue = default)
        => new(defaultValue, false);

    /// <summary>
    /// Creates a new generic optional from a non-generic optional.
    /// </summary>
    public static Optional<T> From(IOptional optional)
    {
        if (optional.HasValue || optional.Value != default)
        {
            return new Optional<T>((T?)optional.Value, optional.HasValue);
        }

        return new Optional<T>();
    }
}
