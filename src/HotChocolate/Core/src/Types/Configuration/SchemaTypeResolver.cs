using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal static class SchemaTypeResolver
    {
        private static readonly Type _keyValuePair = typeof(KeyValuePair<,>);

        public static bool TryInferSchemaType(
            ClrTypeReference unresolvedType,
            out ClrTypeReference schemaType)
        {
            if (IsObjectTypeExtension(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(ObjectTypeExtension<>).MakeGenericType(unresolvedType.Type));
            }
            else if (IsUnionType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(UnionType<>).MakeGenericType(unresolvedType.Type));
            }
            else if (IsInterfaceType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(InterfaceType<>).MakeGenericType(unresolvedType.Type));
            }
            else if (IsObjectType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(ObjectType<>).MakeGenericType(unresolvedType.Type));
            }
            else if (IsInputObjectType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(InputObjectType<>).MakeGenericType(unresolvedType.Type));
            }
            else if (IsEnumType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeof(EnumType<>).MakeGenericType(unresolvedType.Type));
            }
            else
            {
                schemaType = null;
            }

            return schemaType != null;
        }

        private static bool IsObjectType(ClrTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.IsDefined(typeof(ObjectTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsObjectTypeExtension(ClrTypeReference unresolvedType) =>
            unresolvedType.Type.IsDefined(typeof(ExtendObjectTypeAttribute), true);

        private static bool IsUnionType(ClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsDefined(typeof(UnionTypeAttribute), true)
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInterfaceType(ClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsInterface
                || unresolvedType.Type.IsDefined(typeof(InterfaceTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInputObjectType(ClrTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.IsDefined(typeof(InputObjectTypeAttribute), true))
                && !unresolvedType.Type.IsAbstract
                && unresolvedType.Context == TypeContext.Input;
        }

        private static bool IsEnumType(ClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsEnum
                || unresolvedType.Type.IsDefined(typeof(EnumTypeAttribute), true))
                && IsPublic(unresolvedType);
        }

        private static bool IsComplexType(ClrTypeReference unresolvedType)
        {
            bool isComplexType =
                unresolvedType.Type.IsClass
                    && IsPublic(unresolvedType)
                    && unresolvedType.Type != typeof(string);

            if (!isComplexType && unresolvedType.Type.IsGenericType)
            {
                Type typeDefinition = unresolvedType.Type.GetGenericTypeDefinition();
                return typeDefinition == _keyValuePair;
            }

            return isComplexType;
        }

        private static bool IsPublic(ClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsPublic
                || unresolvedType.Type.IsNestedPublic;
        }
    }
}
