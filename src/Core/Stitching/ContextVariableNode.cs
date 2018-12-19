
using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    internal sealed class ScopedVariableNode
        : IValueNode<string>
        , IEquatable<ScopedVariableNode>
    {
        public ScopedVariableNode(
            Language.Location location,
            NameNode scope,
            NameNode name)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Location = location;
            Scope = scope;
            Name = name;
        }

        public NodeKind Kind { get; } = NodeKind.Variable;

        public Language.Location Location { get; }

        public NameNode Scope { get; }

        public NameNode Name { get; }

        public string Value => Scope.Value + ":" + Name.Value;

        /// <summary>
        /// Determines whether the specified <see cref="ScopedVariableNode"/>
        /// is equal to the current <see cref="ScopedVariableNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="ScopedVariableNode"/> to compare with the current
        /// <see cref="ScopedVariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="ScopedVariableNode"/> is equal
        /// to the current <see cref="ScopedVariableNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ScopedVariableNode other)
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
        /// to the current <see cref="ScopedVariableNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="ScopedVariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="ScopedVariableNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IValueNode other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other is ScopedVariableNode v)
            {
                return Equals(v);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="ScopedVariableNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="ScopedVariableNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="ScopedVariableNode"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as ScopedVariableNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="ScopedVariableNode"/>
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
        /// <see cref="ScopedVariableNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="ScopedVariableNode"/>.
        /// </returns>
        public override string ToString()
        {
            return Value;
        }

        public VariableNode ToVariableNode()
        {
            return new VariableNode(new NameNode(ToVariableName()));
        }

        public string ToVariableName()
        {
            return Scope.Value + "_" + Name.Value;
        }
    }
}
