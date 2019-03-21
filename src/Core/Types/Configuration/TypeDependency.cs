using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal sealed class TypeDependency
    {
        public TypeDependency(
            ITypeReference typeReference,
            TypeDependencyKind kind)
        {
            TypeReference = typeReference
                ?? throw new ArgumentNullException(nameof(typeReference));
            Kind = kind;
        }

        public TypeDependencyKind Kind { get; }

        public ITypeReference TypeReference { get; }
    }
}
