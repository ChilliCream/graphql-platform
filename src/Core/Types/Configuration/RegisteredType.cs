using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal sealed class RegisteredType
        : IHasClrType
    {
        public RegisteredType(
            ITypeReference reference,
            TypeSystemObjectBase type,
            IReadOnlyList<TypeDependency> dependencies)
        {
            Reference = reference
                ?? throw new ArgumentNullException(nameof(reference));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            Dependencies = dependencies
                ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public ITypeReference Reference { get; }

        public TypeSystemObjectBase Type { get; }

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

            return new RegisteredType(Reference, Type, dependencies);
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

            return new RegisteredType(Reference, Type, merged);
        }
    }
}
