using System;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        internal static class Tools
        {
            internal static bool IsSchemaType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return Helper.IsSchemaType(type);
            }

            internal static bool IsGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return BaseTypes.IsGenericBaseType(type);
            }

            internal static bool IsNonGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return BaseTypes.IsNonGenericBaseType(type);
            }

            public static Type? GetElementType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return Helper.GetInnerListType(type);
            }

            public static Type? GetNamedType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (BaseTypes.IsGenericBaseType(type))
                {
                    return type;
                }

                if (type.IsGenericType)
                {
                    Type definition = type.GetGenericTypeDefinition();
                    if (typeof(ListType<>) == definition
                        || typeof(NonNullType<>) == definition
                        || typeof(NativeType<>) == definition)
                    {
                        return GetNamedType(type.GetGenericArguments()[0]);
                    }
                }

                return null;
            }
        }
    }
}
