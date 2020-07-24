using System;
using HotChocolate.Properties;
using HotChocolate.Utilities;

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
            return new TypeDependency(
                typeReference ?? TypeReference,
                kind ?? Kind);
        }

        public static TypeDependency FromSchemaType(Type type) =>
            FromSchemaType(type, TypeDependencyKind.Default);

        public static TypeDependency FromSchemaType(
            Type type,
            TypeDependencyKind kind)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (BaseTypes.IsSchemaType(type))
            {
                TypeContext context = SchemaTypeReference.InferTypeContext(type);
                var reference = Descriptors.TypeReference.Create(type, context);
                return new TypeDependency(reference, kind);
            }

            throw new ArgumentException(
                TypeResources.TypeDependency_MustBeSchemaType,
                nameof(type));
        }
    }
}
