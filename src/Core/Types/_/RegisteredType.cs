using System.ComponentModel.Design;
using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
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
            return new RegisteredType(Reference, Type, Dependencies);
        }
    }
}
