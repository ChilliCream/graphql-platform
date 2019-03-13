using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public sealed class TypeReference
        : IEquatable<TypeReference>
    {
        public TypeReference(ITypeNode type)
        {
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public TypeReference(Type nativeType)
            : this(nativeType, TypeContext.Output)
        {
        }

        public TypeReference(Type nativeType, TypeContext context)
        {
            ClrType = nativeType
                ?? throw new ArgumentNullException(nameof(nativeType));
            Context = context;
        }

        public TypeReference(IType schemaType)
        {
            SchemaType = schemaType
                ?? throw new ArgumentNullException(nameof(schemaType));
            Context = TypeContext.None;
        }

        public TypeContext Context { get; }

        public Type ClrType { get; }

        public IType SchemaType { get; }

        public ITypeNode Type { get; }

        public bool Equals(TypeReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Type == null && other.Type == null)
            {
                return ClrType.Equals(other.ClrType)
                    && Context.Equals(other.Context);
            }

            return Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeReference tr)
            {
                return Equals(tr);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (Type == null)
                {
                    return (ClrType.GetHashCode() * 397)
                        ^ (Context.GetHashCode() * 97);
                }
                return Type.GetHashCode();
            }
        }

        public override string ToString() =>
            ClrType == null ? Type.ToString() : ClrType.GetTypeName();
    }

    internal static class TypeReferenceExtensions
    {
        public static bool IsClrTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.ClrType != null;
        }

        public static bool IsSchemaTypeReference(
            this TypeReference typeReference)
        {
            return typeReference.SchemaType != null;
        }
    }
}
