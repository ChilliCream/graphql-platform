using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        private static class SystemType
        {
            public static ExtendedType FromType(Type type, TypeCache cache)
            {
                type = Helper.RemoveNonEssentialTypes(type);

                if (type.IsValueType)
                {
                    if (type.IsGenericType
                        && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return new ExtendedType(
                            type.GetGenericArguments()[0],
                            ExtendedTypeKind.Runtime,
                            typeArguments: GetGenericArguments(type, cache),
                            source: type,
                            isNullable: true);
                    }

                    return new ExtendedType(
                        type,
                        ExtendedTypeKind.Runtime,
                        typeArguments: GetGenericArguments(type, cache),
                        isNullable: false);
                }

                Type? elementType = Helper.GetInnerListType(type);

                return new ExtendedType(
                    type.GetGenericArguments()[0],
                    ExtendedTypeKind.Runtime,
                    typeArguments: GetGenericArguments(type, cache),
                    source: type,
                    definition: typeof(Nullable<>),
                    isNullable: true,
                    isList: Helper.IsListType(type),
                    elementType: elementType is null ? null : FromType(elementType, cache));
            }

            public static IReadOnlyList<ExtendedType> GetGenericArguments(Type type, TypeCache cache)
            {
                if (type.IsGenericType)
                {
                    Type[] arguments = type.GetGenericArguments();
                    ExtendedType[] extendedArguments = new ExtendedType[arguments.Length];

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        extendedArguments[i] = FromType(arguments[i], cache);
                    }

                    return extendedArguments;
                }

                return Array.Empty<ExtendedType>();
            }
        }
    }
}
