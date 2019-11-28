using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration
{
    internal static class SchemaTypeResolver
    {
        private static readonly Type _keyValuePair = typeof(KeyValuePair<,>);

        public static bool TryInferSchemaType(
            IClrTypeReference unresolvedType,
            out IClrTypeReference? schemaType)
        {
            if (IsInterfaceType(unresolvedType))
            {
                schemaType = typeof(InterfaceType<>)
                    .MakeGenericType(unresolvedType.Type.Type)
                    .ToTypeReference(TypeContext.Output);
            }
            else if (IsObjectType(unresolvedType))
            {
                schemaType = typeof(ObjectType<>)
                    .MakeGenericType(unresolvedType.Type.Type)
                    .ToTypeReference(TypeContext.Output);
            }
            else if (IsInputObjectType(unresolvedType))
            {
                schemaType = typeof(InputObjectType<>)
                    .MakeGenericType(unresolvedType.Type.Type)
                    .ToTypeReference(TypeContext.Input);
            }
            else if (IsEnumType(unresolvedType))
            {
                schemaType = typeof(EnumType<>)
                    .MakeGenericType(unresolvedType.Type.Type)
                    .ToTypeReference(unresolvedType.Context);
            }
            else
            {
                schemaType = null;
            }

            return schemaType is { };
        }

        private static bool IsObjectType(IClrTypeReference unresolvedType)
        {
            return IsComplexType(unresolvedType)
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInterfaceType(IClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsInterface
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInputObjectType(IClrTypeReference unresolvedType)
        {
            return IsComplexType(unresolvedType)
                && !unresolvedType.Type.Type.IsAbstract
                && unresolvedType.Context == TypeContext.Input;
        }

        private static bool IsComplexType(IClrTypeReference unresolvedType)
        {
            bool isComplexType =
                (unresolvedType.Type.Type.IsClass
                    && (unresolvedType.Type.Type.IsPublic
                        || unresolvedType.Type.Type.IsNestedPublic))
                    && unresolvedType.Type.Type != typeof(string);

            if (!isComplexType && unresolvedType.Type.IsGeneric)
            {
                return unresolvedType.Type.Definition == _keyValuePair;
            }

            return isComplexType;
        }

        private static bool IsEnumType(IClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.Type.IsEnum
                    && (unresolvedType.Type.Type.IsPublic
                        || unresolvedType.Type.Type.IsNestedPublic));
        }
    }
}
