using System;

namespace HotChocolate.Types.Descriptors
{
    public sealed class SchemaTypeReference
        : TypeReferenceBase
        , ISchemaTypeReference
    {
        public SchemaTypeReference(IType type)
            : this(type, null, null)
        {
        }

        public SchemaTypeReference(
            IType type, bool? isTypeNullable, bool? isElementTypeNullable)
            : base(InferTypeContext(type), isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public IType Type { get; }

        public ISyntaxTypeReference Compile()
        {
            throw new NotImplementedException();
        }

        public bool Equals(SchemaTypeReference other)
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

        public bool Equals(ISchemaTypeReference other)
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

        public static TypeContext InferTypeContext(IType type)
        {
            INamedType namedType = type.NamedType();

            if (namedType.IsInputType() && namedType.IsOutputType())
            {
                return TypeContext.None;
            }
            else if (namedType.IsOutputType())
            {
                return TypeContext.Output;
            }
            else if (namedType.IsInputType())
            {
                return TypeContext.Input;
            }
            else
            {
                return TypeContext.None;
            }
        }
    }
}
