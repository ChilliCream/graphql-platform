using System;

namespace HotChocolate.Types.Descriptors
{
    public sealed class SchemaTypeReference
        : TypeReferenceBase
        , ISchemaTypeReference
    {
        public SchemaTypeReference(ITypeSystemMember type)
            : this(type, null, null)
        {
        }

        public SchemaTypeReference(
            ITypeSystemMember type,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
            : base(InferTypeContext(type),
                isTypeNullable,
                isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public ITypeSystemMember Type { get; }

        public ISchemaTypeReference WithType(ITypeSystemMember type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return new SchemaTypeReference(type);
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

            if (Type == other.Type
                || (Type is IType a && other.Type is IType b && a.IsEqualTo(b)))
            {
                return Context == other.Context
                    && IsTypeNullable.Equals(other.IsTypeNullable)
                    && IsElementTypeNullable.Equals(
                        other.IsElementTypeNullable);
            }

            return false;
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

            if (Type == other.Type
                || (Type is IType a && other.Type is IType b && a.IsEqualTo(b)))
            {
                return Context == other.Context
                    && IsTypeNullable.Equals(other.IsTypeNullable)
                    && IsElementTypeNullable.Equals(other.IsElementTypeNullable);
            }

            return false;
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

            if (obj is SchemaTypeReference str)
            {
                return Equals(str);
            }

            if (obj is ISchemaTypeReference istr)
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

        internal static TypeContext InferTypeContext(object type)
        {
            if (type is IType t)
            {
                INamedType namedType = t.NamedType();

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
            }

            return TypeContext.None;
        }

        internal static TypeContext InferTypeContext(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeof(IInputType).IsAssignableFrom(type)
                && typeof(IOutputType).IsAssignableFrom(type))
            {
                return TypeContext.None;
            }
            else if (typeof(IOutputType).IsAssignableFrom(type))
            {
                return TypeContext.Output;
            }
            else if (typeof(IInputType).IsAssignableFrom(type))
            {
                return TypeContext.Input;
            }
            else
            {
                return TypeContext.None;
            }
        }

        internal static SchemaTypeReference Create<T>(T type)
            where T : ITypeSystemMember
        {
            return new SchemaTypeReference(type);
        }
    }
}
