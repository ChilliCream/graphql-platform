using System;
using System.ComponentModel;
using System.Globalization;
using HotChocolate.Properties;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The type name string guarantees that a string adheres to the
    /// GraphQL spec rules: /[_A-Za-z][_0-9A-Za-z]*/
    /// </summary>
    [TypeConverter(typeof(MultiplierPathStringConverter))]
    public struct MultiplierPathString
        : IEquatable<MultiplierPathString>
    {

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="MultiplierPathString"/> struct.
        /// </summary>
        /// <param name="value">The actual type name string</param>
        /// <exception cref="ArgumentException"
        public MultiplierPathString(string value)
        {
            if (!IsValidPath(value))
            {
                throw new ArgumentException(
                    TypeResourceHelper.Type_Name_IsNotValid(value),
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
        /// Appends a <see cref="MultiplierPathString"/> to this
        /// instance and returns a new instance of
        /// <see cref="MultiplierPathString"/> representing the combined
        /// <see cref="MultiplierPathString"/>.
        /// </summary>
        /// <returns>The combined <see cref="MultiplierPathString"/>.</returns>
        public MultiplierPathString Add(MultiplierPathString other)
        {
            return new MultiplierPathString(Value + other.Value);
        }

        /// <summary>
        /// Compares this <see cref="MultiplierPathString"/> value
        /// to another value using a specific <see cref="StringComparison"/>
        /// type.
        /// </summary>
        /// <param name="other">
        /// The second <see cref="MultiplierPathString"/> for comparison.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison"/> type to use.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="MultiplierPathString"/> values
        /// are equal.
        /// </returns>
        public bool Equals(
            MultiplierPathString other,
            StringComparison comparisonType)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(Value, other.Value, comparisonType);
        }

        /// <summary>
        /// Compares this <see cref="MultiplierPathString"/> value to
        /// another value using <see cref=" StringComparison.Ordinal"/>
        /// comparison type.
        /// </summary>
        /// <param name="other">
        /// The second <see cref="MultiplierPathString"/> for comparison.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="MultiplierPathString"/>
        /// values are equal.
        /// </returns>
        public bool Equals(MultiplierPathString other) =>
            Equals(other, StringComparison.Ordinal);

        /// <summary>
        /// Compares this <see cref="MultiplierPathString"/> value to
        /// another value using <see cref=" StringComparison.Ordinal"/>
        /// comparison.
        /// </summary>
        /// <param name="obj">
        /// The second <see cref="MultiplierPathString"/> for comparison.
        /// </param>
        /// <returns>
        /// <c>true</c> if both <see cref="MultiplierPathString"/> values
        /// are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return IsEmpty;
            }
            return obj is MultiplierPathString n && Equals(n);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="MultiplierPathString"/>
        /// object.
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
        /// <c>true</c> if both <see cref="MultiplierPathString"/>
        /// values are equal.
        /// </returns>
        public static bool operator ==(
            MultiplierPathString left,
            MultiplierPathString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// <c>true</c> if both <see cref="MultiplierPathString"/> values
        /// are not equal.
        /// </returns>
        public static bool operator !=(
            MultiplierPathString left,
            MultiplierPathString right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(string left, MultiplierPathString right)
        {
            // This overload exists to prevent the implicit
            // string<->MultiplierPathString
            // converter from trying to call the
            // MultiplierPathString+MultiplierPathString operator
            // for things that are not name strings.
            return string.Concat(left, right.ToString());
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(MultiplierPathString left, string right)
        {
            // This overload exists to prevent the implicit
            // string<->MultiplierPathString
            // converter from trying to call the
            // MultiplierPathString+MultiplierPathString operator
            // for things that are not name strings.
            return string.Concat(left.ToString(), right);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>
        /// The <see cref="MultiplierPathString"/> combination of both values
        /// </returns>
        public static MultiplierPathString operator +(
            MultiplierPathString left,
            MultiplierPathString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Implicitly creates a new <see cref="MultiplierPathString"/> from
        /// the given string.
        /// </summary>
        /// <param name="s">The string.</param>
        public static implicit operator MultiplierPathString(string s)
            => ConvertFromString(s);

        /// <summary>
        /// Implicitly calls ToString().
        /// </summary>
        /// <param name="name"></param>
        public static implicit operator string(MultiplierPathString name)
            => name.ToString();

        internal static MultiplierPathString ConvertFromString(string s)
            => string.IsNullOrEmpty(s)
                ? new MultiplierPathString()
                : new MultiplierPathString(s);

        public static bool IsValidPath(string name)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }

            if (name[0].IsLetterOrUnderscore())
            {
                if (name.Length > 1)
                {
                    for (int i = 1; i < name.Length; i++)
                    {
                        if (!name[i].IsLetterOrDigitOrUnderscore()
                            && name[i] != GraphQLConstants.Dot)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public static bool IsValidPath(ReadOnlySpan<byte> name)
        {
            if (name == null || name.Length == 0)
            {
                return false;
            }

            if (name[0].IsLetterOrUnderscore())
            {
                if (name.Length > 1)
                {
                    for (int i = 1; i < name.Length; i++)
                    {
                        if (!name[i].IsLetterOrDigitOrUnderscore()
                            && name[i] != GraphQLConstants.Dot)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }

    internal class MultiplierPathStringConverter
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
                    ? MultiplierPathString.ConvertFromString((string)value)
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

    public static class MultiplierPathStringExtensions
    {
        public static MultiplierPathString EnsureNotEmpty(
            this MultiplierPathString name,
            string argumentName)
        {
            if (name.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_Cannot_BeEmpty,
                    argumentName);
            }

            return name;
        }
    }
}
