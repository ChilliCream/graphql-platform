using System;
using System.ComponentModel;
using System.Globalization;
using HotChocolate.Utilities;

namespace HotChocolate
{
    /// <summary>
    /// The type name string guarantees that a string adheres to the
    /// GraphQL spec rules: /[_A-Za-z][_0-9A-Za-z]*/
    /// </summary>
    [TypeConverter(typeof(NameStringConverter))]
    public struct NameString
        : IEquatable<NameString>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NameString"/> struct.
        /// </summary>
        /// <param name="value">The actual type name string</param>
        /// <exception cref="ArgumentException"
        public NameString(string value)
        {
            if (!NameUtils.IsValidName(value))
            {
                throw new ArgumentException(
                    AbstractionResources.Type_Name_IsNotValid(value),
                    nameof(value));
            }
            Value = value;
        }

        /// <summary>
        /// The name value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// <c>true</c> if the name is not empty
        /// </summary>
        public bool HasValue
        {
            get
            {
                return !IsEmpty;
            }
        }

        public bool IsEmpty
        {
            get => string.IsNullOrEmpty(Value);
        }

        /// <summary>
        /// Provides the name string.
        /// </summary>
        /// <returns>The name string value</returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Appends a <see cref="NameString"/> to this
        /// instance and returns a new instance of <see cref="NameString"/>
        /// representing the combined <see cref="NameString"/>.
        /// </summary>
        /// <returns>The combined <see cref="NameString"/>.</returns>
        public NameString Add(NameString other)
        {
            return new NameString(Value + other.Value);
        }

        /// <summary>
        /// Compares this <see cref="NameString"/> value to another value
        /// using a specific <see cref="StringComparison"/> type.
        /// </summary>
        /// <param name="other">
        /// The second <see cref="NameString"/> for comparison.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison"/> type to use.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="NameString"/> values are equal.
        /// </returns>
        public bool Equals(NameString other, StringComparison comparisonType)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(Value, other.Value, comparisonType);
        }

        /// <summary>
        /// Compares this <see cref="NameString"/> value to another value using
        /// <see cref=" StringComparison.Ordinal"/> comparison type.
        /// </summary>
        /// <param name="other">
        /// The second <see cref="NameString"/> for comparison.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="NameString"/> values are equal.
        /// </returns>
        public bool Equals(NameString other) =>
            Equals(other, StringComparison.Ordinal);

        /// <summary>
        /// Compares this <see cref="NameString"/> value to another value using
        /// <see cref=" StringComparison.Ordinal"/> comparison.
        /// </summary>
        /// <param name="obj">
        /// The second <see cref="NameString"/> for comparison.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="NameString"/> values are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return IsEmpty;
            }
            return obj is NameString n && Equals(n);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="NameString"/> object.
        /// </summary>
        /// <returns>
        /// A hash code for this instance that is suitable for use in hashing
        /// algorithms and data structures such as a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return (HasValue ? StringComparer.Ordinal.GetHashCode(Value) : 0);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// <c>true</c> if both <see cref="NameString"/> values are equal.
        /// </returns>
        public static bool operator ==(NameString left, NameString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// <c>true</c> if both <see cref="NameString"/> values are not equal.
        /// </returns>
        public static bool operator !=(NameString left, NameString right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(string left, NameString right)
        {
            // This overload exists to prevent the implicit string<->NameString
            // converter from trying to call the NameString+NameString operator
            // for things that are not name strings.
            return string.Concat(left, right.ToString());
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(NameString left, string right)
        {
            // This overload exists to prevent the implicit string<->NameString
            // converter from trying to call the NameString+NameString operator
            // for things that are not name strings.
            return string.Concat(left.ToString(), right);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// The <see cref="NameString"/> combination of both values
        /// </returns>
        public static NameString operator +(NameString left, NameString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Implicitly creates a new <see cref="NameString"/> from
        /// the given string.
        /// </summary>
        /// <param name="s">The string.</param>
        public static implicit operator NameString(string s)
            => ConvertFromString(s);

        /// <summary>
        /// Implicitly calls ToString().
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator string(NameString name)
            => name.ToString();

        internal static NameString ConvertFromString(string s)
            => string.IsNullOrEmpty(s)
                ? new NameString()
                : new NameString(s);
    }

    internal class NameStringConverter
        : TypeConverter
    {
        public override bool CanConvertFrom(
            ITypeDescriptorContext context,
            Type sourceType) =>
                sourceType == typeof(string)
                || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value) =>
                value is string
                    ? NameString.ConvertFromString((string)value)
                    : base.ConvertFrom(context, culture, value);

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType) =>
                destinationType == typeof(string)
                    ? value.ToString()
                    : base.ConvertTo(context, culture, value, destinationType);
    }

    public static class NameStringExtensions
    {
        public static NameString EnsureNotEmpty(
            this NameString name,
            string argumentName)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    AbstractionResources.Name_Cannot_BeEmpty(),
                    argumentName);
            }

            return name;
        }
    }
}
