using System;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public static class TypeDescriptors
    {
        private static ITypeDescriptor Unwrap(
            IType type,
            Func<INamedType, INamedTypeDescriptor> mapNamedType)
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
