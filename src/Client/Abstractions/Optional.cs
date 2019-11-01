using System;
using System.ComponentModel;
using System.Globalization;

namespace StrawberryShake
{
    /// <summary>
    /// The optional type is used to differentiate between not set and set input values.
    /// </summary>
    public readonly struct Optional<T>
        : IEquatable<Optional<T>>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Optional{T}"/> struct.
        /// </summary>
        /// <param name="value">The actual value.</param>
        public Optional(T value)
        {
            Value = value;
            HasValue = true;
        }

        /// <summary>
        /// The name value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// <c>true</c> if the optional has a value.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// <c>true</c> if the optional has no value.
        /// </summary>
        public bool IsEmpty => !HasValue;

        /// <summary>
        /// Provides the name string.
        /// </summary>
        /// <returns>The name string value</returns>
        public override string? ToString()
        {
            return Value?.ToString();
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

            return object.Equals(Value, other.Value);
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
        /// Serves as a hash function for a <see cref="Optional"/> object.
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
        /// <c>true</c> if both <see cref="Optional"/> values are equal.
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
        /// <c>true</c> if both <see cref="Optional"/> values are not equal.
        /// </returns>
        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Implicitly creates a new <see cref="Optional"/> from
        /// the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static implicit operator Optional<T>(T value)
            => new Optional<T>(value);

        /// <summary>
        /// Implicitly gets the optional value.
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator T(Optional<T> optional)
            => optional.Value;
    }
}
