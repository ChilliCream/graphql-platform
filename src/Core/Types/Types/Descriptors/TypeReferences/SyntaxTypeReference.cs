using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public sealed class SyntaxTypeReference
        : TypeReferenceBase
        , ISyntaxTypeReference
        , IEquatable<SyntaxTypeReference>
        , IEquatable<ISyntaxTypeReference>
    {
        public SyntaxTypeReference(ITypeNode type, TypeContext context)
            : this(type, context, null, null)
        {
        }

        public SyntaxTypeReference(
            ITypeNode type,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
            : base(context, isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public ITypeNode Type { get; }

        public ISyntaxTypeReference WithType(ITypeNode type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return new SyntaxTypeReference(type, Context);
        }

        public bool Equals(SyntaxTypeReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type.IsEqualTo(other.Type)
                && Context == other.Context
                && IsTypeNullable.Equals(other.IsTypeNullable)
                && IsElementTypeNullable.Equals(other.IsElementTypeNullable);
        }

        public bool Equals(ISyntaxTypeReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type.IsEqualTo(other.Type)
                && Context == other.Context
                && IsTypeNullable.Equals(other.IsTypeNullable)
                && IsElementTypeNullable.Equals(other.IsElementTypeNullable);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is SyntaxTypeReference str)
            {
                return Equals(str);
            }

            if (obj is ISyntaxTypeReference istr)
            {
                return Equals(istr);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Type.GetHashCode() * 397;
                hash = hash ^ (Context.GetHashCode() * 7);
                hash = hash ^ (IsTypeNullable?.GetHashCode() ?? 0 * 11);
                hash = hash ^ (IsElementTypeNullable?.GetHashCode() ?? 0 * 13);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Context}: {Type}";
        }
    }
}
