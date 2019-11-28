using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Utilities
{
    public static class ExtendedTypeExtensions
    {
        public static IExtendedType ToExtendedType(this Type type)
        {
            return ExtendedType.FromType(type);
        }

        public static IClrTypeReference ToTypeReference(
            this IExtendedType type,
            TypeContext context = TypeContext.None)
        {
            return new ClrTypeReference(type, context);
        }

        public static IClrTypeReference ToTypeReference(
            this Type type,
            TypeContext context = TypeContext.None)
        {
            return new ClrTypeReference(type.ToExtendedType(), context);
        }
    }
}
