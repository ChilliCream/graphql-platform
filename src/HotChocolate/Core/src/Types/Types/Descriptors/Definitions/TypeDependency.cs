using System;
using HotChocolate.Internal;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public sealed class TypeDependency
    {
        public TypeDependency(
            ITypeReference typeReference,
            TypeDependencyKind kind = TypeDependencyKind.Default)
        {
            TypeReference = typeReference ??
                throw new ArgumentNullException(nameof(typeReference));
            Kind = kind;
        }

        public TypeDependencyKind Kind { get; }

        public ITypeReference TypeReference { get; }

        public TypeDependency With(
            ITypeReference? typeReference = null,
            TypeDependencyKind? kind = null)
        {
            return new(
                typeReference ?? TypeReference,
                kind ?? Kind);
        }

        public static TypeDependency FromSchemaType(
            IExtendedType type,
            TypeDependencyKind kind = TypeDependencyKind.Default)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsSchemaType)
            {
                throw new ArgumentException(
                    TypeResources.TypeDependency_MustBeSchemaType,
                    nameof(type));
            }

            return new TypeDependency(
                Descriptors.TypeReference.Create(type),
                kind);
        }
    }
}
