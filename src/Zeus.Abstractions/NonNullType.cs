using System;

namespace Zeus.Abstractions
{
    public sealed class NonNullType
        : IType
        , IEquatable<IType>
        , IEquatable<NonNullType>
    {
        public NonNullType(IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type is NonNullType)
            {
                throw new ArgumentException("Only list and named types are allowed.", nameof(type));
            }

            Type = type;
        }

        public IType Type { get; }

        public bool Equals(NonNullType other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Type.Equals(Type);
        }

        public bool Equals(IType other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return Equals(other as NonNullType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as NonNullType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 397 ^ (7 * Type.GetHashCode());
            }
        }

        public override string ToString()
        {
            return Type.ToString() + "!";
        }
    }
}