using System.Text;
using System;

namespace HotChocolate.Language
{
    public sealed class VariableNode
        : IValueNode<string>
        , IEquatable<VariableNode>
    {
        private ReadOnlyMemory<byte> _memory;

        public VariableNode(string name)
            : this(null, new NameNode(name))
        {
        }

        public VariableNode(NameNode name)
            : this(null, name)
        {
        }

        public VariableNode(
            Location? location,
            NameNode name)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public NodeKind Kind { get; } = NodeKind.Variable;

        public Location? Location { get; }

        public NameNode Name { get; }

        public string Value => Name.Value;

        object IValueNode.Value => Value;

        /// <summary>
        /// Determines whether the specified <see cref="VariableNode"/>
        /// is equal to the current <see cref="VariableNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="VariableNode"/> to compare with the current
        /// <see cref="VariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="VariableNode"/> is equal
        /// to the current <see cref="VariableNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(VariableNode? other)
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
        /// to the current <see cref="VariableNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="VariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="VariableNode"/>;
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

            if (other is VariableNode v)
            {
                return Equals(v);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="VariableNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="VariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="VariableNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as VariableNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="VariableNode"/>
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
        /// Returns a <see cref="string"/> that represents the current
        /// <see cref="VariableNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="VariableNode"/>.
        /// </returns>
        public override string? ToString()
        {
            return Value;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            if (_memory.IsEmpty)
            {
                _memory = Encoding.UTF8.GetBytes(Value);
            }
            return _memory.Span;
        }

        public VariableNode WithLocation(Location? location)
        {
            return new VariableNode(location, Name);
        }

        public VariableNode WithName(NameNode name)
        {
            return new VariableNode(Location, name);
        }
    }
}
