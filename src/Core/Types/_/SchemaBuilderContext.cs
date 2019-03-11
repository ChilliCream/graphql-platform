using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
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


    internal sealed class RegisteredType
        : IHasClrType
    {
        public RegisteredType(TypeSystemObjectBase type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

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

        public ICollection<TypeDependency> Dependencies { get; } =
            new List<TypeDependency>();
    }

}
