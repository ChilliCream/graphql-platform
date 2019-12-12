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
            IReadOnlyList<TypeDependency> dependencies,
            bool isAutoInferred)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            References = new[] { reference };
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Dependencies = dependencies
                ?? throw new ArgumentNullException(nameof(dependencies));
            IsAutoInferred = isAutoInferred;
        }

        public RegisteredType(
            IReadOnlyList<ITypeReference> references,
            TypeSystemObjectBase type,
            IReadOnlyList<TypeDependency> dependencies,
            bool isAutoInferred)
        {
            References = references
                ?? throw new ArgumentNullException(nameof(references));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Dependencies = dependencies
                ?? throw new ArgumentNullException(nameof(dependencies));
            IsAutoInferred = isAutoInferred;
        }

        public IReadOnlyList<ITypeReference> References { get; }

        public TypeSystemObjectBase Type { get; }

        public bool IsAutoInferred { get; }

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
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            return new RegisteredType(References, Type, dependencies, IsAutoInferred);
        }

        public RegisteredType AddDependencies(
            IReadOnlyList<TypeDependency> dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            var merged = Dependencies.ToList();
            merged.AddRange(dependencies);

            return new RegisteredType(References, Type, merged, IsAutoInferred);
        }

        public void Update(IDictionary<ITypeReference, RegisteredType> types)
        {
            foreach (ITypeReference reference in References)
            {
                types[reference] = this;
            }
        }
    }
}
