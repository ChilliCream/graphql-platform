using System;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents an entry in a <see cref="IResultMap"/>
    /// </summary>
    public readonly struct ResultValue : IEquatable<ResultValue>
    {
        /// <summary>
        /// Creates a new result value.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="value">The value of the entry.</param>
        /// <param name="isNullable">
        /// Specifies if the <paramref name="value"/> is allowed to be null.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <see cref="name"/> is <c>null</c> or <see cref="String.Empty"/>.
        /// </exception>
        public ResultValue(string name, object? value, bool isNullable = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    AbstractionResources.ResultValue_NameIsNullOrEmpty,
                    nameof(name));
            }

            Name = name;
            Value = value;
            IsNullable = isNullable;
        }

        /// <summary>
        /// Gets the name of this <see cref="IResultMap"/> entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of this <see cref="IResultMap"/> entry.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Specifies if <see cref="Value"/> is allowed to be empty.
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Specifies if this entry is fully initialized.
        /// </summary>
        public bool IsInitialized => Name is not null;

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
        /// and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj)
            => obj is ResultValue value && Equals(value);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the
        /// <paramref name="other" /> parameter; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(ResultValue other)
        {
            if (IsInitialized != other.IsInitialized)
            {
                return false;
            }

            if (IsInitialized == false)
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                   Equals(Value, other.Value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (Name?.GetHashCode() ?? 0) * 3;
                hash ^= (Value?.GetHashCode() ?? 0) * 7;
                return hash;
            }
        }
    }
}
