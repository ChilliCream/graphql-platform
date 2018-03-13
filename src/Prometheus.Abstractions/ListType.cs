using System;

namespace Prometheus.Abstractions
{
    public sealed class ListType
       : IType
       , IEquatable<IType>
       , IEquatable<ListType>
    {
        public ListType(IType elementType)
        {
            if (elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (elementType is ListType)
            {
                throw new ArgumentException("Only non-null and named types are allowed.", nameof(elementType));
            }

            ElementType = elementType;
        }

        public IType ElementType { get; }

        public bool Equals(ListType other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.ElementType.Equals(ElementType);
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

            return Equals(other as NamedType);
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

            return Equals(obj as NamedType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 397 ^ (7 * ElementType.GetHashCode());
            }
        }

        public override string ToString()
        {
            return "[" + ElementType.ToString() + "]";
        }
    }
}