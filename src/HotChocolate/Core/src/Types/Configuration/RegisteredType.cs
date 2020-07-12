using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class RegisteredType
        : IHasRuntimeType
    {
        public RegisteredType(
            IReadOnlyList<ITypeReference> references,
            TypeSystemObjectBase type,
            TypeDiscoveryContext discoveryContext,
            IReadOnlyList<TypeDependency> dependencies,
            bool isInferred)
        {
            References = references;
            Type = type;
            DiscoveryContext = discoveryContext;
            Dependencies = dependencies;
            IsInferred = isInferred;
            IsExtension = Type is INamedTypeExtensionMerger;
            IsNamedType = Type is INamedType;
            IsDirectiveType = Type is DirectiveType;
        }

        public IReadOnlyList<ITypeReference> References { get; }

        public TypeSystemObjectBase Type { get; }

        public TypeDiscoveryContext DiscoveryContext { get; }

        public bool IsInferred { get; }

        public bool IsExtension { get; }

        public bool IsNamedType { get; }

        public bool IsDirectiveType { get; }

        public Type RuntimeType
        {
            get
            {
                if (Type is IHasRuntimeType hasClrType)
                {
                    return hasClrType.RuntimeType;
                }
                return typeof(object);
            }
        }

        public IReadOnlyList<TypeDependency> Dependencies { get; }

        public RegisteredType WithDependencies(
            IReadOnlyList<TypeDependency> dependencies)
        {
            return new RegisteredType(
                References,
                Type,
                DiscoveryContext,
                dependencies,
                IsInferred);
        }

        public RegisteredType AddDependencies(
            IReadOnlyList<TypeDependency> dependencies)
        {
            var merged = Dependencies.ToList();
            merged.AddRange(dependencies);

            return new RegisteredType(
                References,
                Type,
                DiscoveryContext,
                merged,
                IsInferred);
        }
    }
}
