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
            bool isAutoInferred)
        {
            References = new[] { reference };
            Type = type;
            InitializationContext = initializationContext;
            Dependencies = dependencies;
            IsInferred = isAutoInferred;
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
        }

        public IReadOnlyList<ITypeReference> References { get; }

        public TypeSystemObjectBase Type { get; }

        public InitializationContext InitializationContext { get; }

        public bool IsInferred { get; }

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
