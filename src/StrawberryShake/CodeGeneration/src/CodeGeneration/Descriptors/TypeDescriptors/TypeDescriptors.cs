using System;
using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration
{
    public static class TypeDescriptors
    {
        private static ITypeDescriptor Unwrap(
            IType type,
            Func<INamedType, NamedTypeDescriptor> mapNamedType)
        {
            switch (type)
            {
                case NonNullType descriptor:
                    return new NonNullTypeDescriptor(Unwrap(descriptor.InnerType(), mapNamedType));
                case ListType descriptor:
                    return new ListTypeDescriptor(Unwrap(descriptor.InnerType(), mapNamedType));
                case INamedType descriptor:
                    return mapNamedType(descriptor);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}