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
            ITypeInspector typeInspector,
            ExtendedTypeReference unresolvedType,
            out ExtendedTypeReference schemaType)
        {
            if (IsObjectTypeExtension(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(ObjectTypeExtension<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else if (IsUnionType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(UnionType<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else if (IsInterfaceType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(InterfaceType<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else if (IsObjectType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(ObjectType<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else if (IsInputObjectType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(InputObjectType<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else if (IsEnumType(unresolvedType))
            {
                schemaType = unresolvedType.With(
                    type: typeInspector.GetType(
                        typeof(EnumType<>).MakeGenericType(unresolvedType.Type.Type)));
            }
            else
            {
                schemaType = null;
            }

            return schemaType != null;
        }

        private static bool IsObjectType(ExtendedTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.Type.IsDefined(typeof(ObjectTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsObjectTypeExtension(ExtendedTypeReference unresolvedType) =>
            unresolvedType.Type.Type.IsDefined(typeof(ExtendObjectTypeAttribute), true);

        private static bool IsUnionType(ExtendedTypeReference unresolvedType)
        {
            return unresolvedType.Type.Type.IsDefined(typeof(UnionTypeAttribute), true)
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInterfaceType(ExtendedTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsInterface
                || unresolvedType.Type.Type.IsDefined(typeof(InterfaceTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInputObjectType(ExtendedTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.Type.IsDefined(typeof(InputObjectTypeAttribute), true))
                && !unresolvedType.Type.Type.IsAbstract
                && unresolvedType.Context == TypeContext.Input;
        }

        private static bool IsEnumType(ExtendedTypeReference unresolvedType)
        {
            return (unresolvedType.Type.Type.IsEnum
                || unresolvedType.Type.Type.IsDefined(typeof(EnumTypeAttribute), true))
                && IsPublic(unresolvedType);
        }

        private static bool IsComplexType(ExtendedTypeReference unresolvedType)
        {
            bool isComplexType =
                unresolvedType.Type.Type.IsClass
                    && IsPublic(unresolvedType)
                    && unresolvedType.Type.Type != typeof(string);

            if (!isComplexType && unresolvedType.Type.IsGeneric)
            {
                Type typeDefinition = unresolvedType.Type.Definition;
                return typeDefinition == _keyValuePair;
            }

            return isComplexType;
        }

        private static bool IsPublic(ExtendedTypeReference unresolvedType)
        {
            return unresolvedType.Type.Type.IsPublic
                   || unresolvedType.Type.Type.IsNestedPublic;
        }
    }
}
