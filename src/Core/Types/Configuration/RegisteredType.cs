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
        : IHasClrType
    {
        public RegisteredType(
            ITypeReference reference,
            TypeSystemObjectBase type,
            InitializationContext initializationContext,
            IReadOnlyList<TypeDependency> dependencies,
            bool isInferred)
            : this(new[] { reference }, type, initializationContext, dependencies, isInferred)
        {
        }

        public RegisteredType(
            IReadOnlyList<ITypeReference> references,
            TypeSystemObjectBase type,
            InitializationContext initializationContext,
            IReadOnlyList<TypeDependency> dependencies,
            bool isInferred)
        {
            References = references;
            Type = type;
            InitializationContext = initializationContext;
            Dependencies = dependencies;
            IsInferred = isInferred;
            IsExtension = Type is INamedTypeExtensionMerger;
            IsNamedType = Type is INamedType;
            IsDirectiveType = Type is DirectiveType;
        }

        public IReadOnlyList<ITypeReference> References { get; }

        public TypeSystemObjectBase Type { get; }

        public InitializationContext InitializationContext { get; }

        public bool IsInferred { get; }

        public bool IsExtension { get; }

        public bool IsNamedType { get; }

        public bool IsDirectiveType { get; }

        public Type ClrType
        {
            get
            {
                if (Type is IHasClrType hasClrType)
                {
                    return hasClrType.ClrType;
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
                InitializationContext,
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
                InitializationContext,
                merged,
                IsInferred);
        }
    }
}
