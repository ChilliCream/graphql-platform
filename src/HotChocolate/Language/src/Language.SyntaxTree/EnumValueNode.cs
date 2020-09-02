using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a enum value literal.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Enum-Value
    /// </summary>
    public sealed class EnumValueNode
        : IValueNode<string>
        , IHasSpan
        , IEquatable<EnumValueNode?>
    {
        private ReadOnlyMemory<byte> _memory;
        private string? _value;

        public EnumValueNode(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            string? stringValue = value.ToString()?.ToUpperInvariant();

            if (stringValue is null)
            {
                throw new ArgumentException(
                    "The value string representation mustn't be null.",
                    nameof(value));
            }

            _value = stringValue;
        }

        public EnumValueNode(string value)
            : this(null, value)
        {
        }

        public EnumValueNode(Location? location, string value)
        {
            Location = location;
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public EnumValueNode(Location? location, ReadOnlyMemory<byte> value)
        {
            if (value.IsEmpty)
            {
                throw new ArgumentNullException(
                    "The value mustn't be empty.",
                    nameof(value));
            }

            Location = location;
            _memory = value;
        }

        public SyntaxKind Kind { get; } = SyntaxKind.EnumValue;

        public Location? Location { get; }

        public unsafe string Value
        {
            get
            {
                if (_value is null)
                {
                    fixed (byte* b = _memory.Span)
                    {
                        _value = Encoding.UTF8.GetString(b, _memory.Span.Length);
                    }
                }
                return _value;
            }
        }

        object IValueNode.Value => Value;

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

        /// <summary>
        /// Determines whether the specified <see cref="EnumValueNode"/>
        /// is equal to the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="EnumValueNode"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="EnumValueNode"/> is equal
        /// to the current <see cref="EnumValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(EnumValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Value.Equals(Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="EnumValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other is EnumValueNode e)
            {
                return Equals(e);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="EnumValueNode"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as EnumValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="EnumValueNode"/>
        /// object.
        /// </summary>
        /// <returns>
        /// A hash code for this instance that is suitable for use in
        /// hashing algorithms and data structures such as a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Kind.GetHashCode() * 397)
                 ^ (Value.GetHashCode() * 97);
            }
        }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public unsafe ReadOnlySpan<byte> AsSpan()
        {
            if (_memory.IsEmpty)
            {
                int length = checked(_value!.Length * 4);
                Span<byte> span = stackalloc byte[length];

                fixed (char* c = _value)
                fixed (byte* b = span)
                {
                    int buffered = Encoding.UTF8.GetBytes(c, _value.Length, b, span.Length);

                    Memory<byte> memory = new byte[buffered];
                    span.Slice(0, buffered).CopyTo(memory.Span);

                    _memory = memory;
                }
            }

            return _memory.Span;
        }

        public EnumValueNode WithLocation(Location? location)
        {
            return new EnumValueNode(location, Value);
        }

        public EnumValueNode WithValue(string value)
        {
            return new EnumValueNode(Location, value);
        }

        public EnumValueNode WithValue(Memory<byte> value)
        {
            return new EnumValueNode(Location, value);
        }
    }
}
