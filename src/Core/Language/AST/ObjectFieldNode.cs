using System;

namespace HotChocolate.Language
{
    public sealed class ObjectFieldNode
        : ISyntaxNode
        , IEquatable<ObjectFieldNode>
    {
        private int? _hash;

        public ObjectFieldNode(string name, bool value)
            : this(null, new NameNode(name), new BooleanValueNode(value))
        {
        }

        public ObjectFieldNode(string name, int value)
            : this(null, new NameNode(name), new IntValueNode(value))
        {
        }

        public ObjectFieldNode(string name, double value)
            : this(null, new NameNode(name), new FloatValueNode(value))
        {
        }

        public ObjectFieldNode(string name, string value)
            : this(null, new NameNode(name), new StringValueNode(value))
        {
        }


        public ObjectFieldNode(string name, IValueNode value)
            : this(null, new NameNode(name), value)
        {
        }

        public ObjectFieldNode(
            Location? location,
            NameNode name,
            IValueNode value)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NodeKind Kind { get; } = NodeKind.ObjectField;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IValueNode Value { get; }

        /// <summary>
        /// Determines whether the specified <see cref="ObjectFieldNode"/>
        /// is equal to the current <see cref="ObjectFieldNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="ObjectFieldNode"/> to compare with the current
        /// <see cref="ObjectFieldNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="ObjectFieldNode"/> is equal
        /// to the current <see cref="ObjectFieldNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ObjectFieldNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Name.Equals(Name) && other.Value.Equals(Value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="ObjectFieldNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="ObjectFieldNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="ObjectFieldNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as ObjectFieldNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="ObjectFieldNode"/>
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
                if (_hash == null)
                {
                    _hash = (Kind.GetHashCode() * 397)
                        ^ (Name.GetHashCode() * 97)
                        ^ (Value.GetHashCode() * 7);
                }

                return _hash.Value;
            }
        }

        public ObjectFieldNode WithLocation(Location? location)
        {
            return new ObjectFieldNode(location, Name, Value);
        }

        public ObjectFieldNode WithName(NameNode name)
        {
            return new ObjectFieldNode(Location, name, Value);
        }

        public ObjectFieldNode WithValue(IValueNode value)
        {
            return new ObjectFieldNode(Location, Name, value);
        }
    }
}
