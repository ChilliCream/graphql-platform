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
            IClrTypeReference unresolvedType,
            out IClrTypeReference schemaType)
        {
            if (IsObjectTypeExtension(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(ObjectTypeExtension<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Output);
            }
            else if (IsUnionType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(UnionType<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Output);
            }
            else if (IsInterfaceType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(InterfaceType<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Output);
            }
            else if (IsObjectType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(ObjectType<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Output);
            }
            else if (IsInputObjectType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(InputObjectType<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Input);
            }
            else if (IsEnumType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(EnumType<>)
                    .MakeGenericType(unresolvedType.Type),
                    unresolvedType.Context);
            }
            else if (Scalars.TryGetScalar(unresolvedType.Type, out schemaType))
            {
                return true;
            }
            else
            {
                schemaType = null;
            }

            return schemaType != null;
        }

        private static bool IsObjectType(IClrTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.IsDefined(typeof(ObjectTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsObjectTypeExtension(IClrTypeReference unresolvedType) =>
            unresolvedType.Type.IsDefined(typeof(ExtendObjectTypeAttribute), true);

        private static bool IsUnionType(IClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsDefined(typeof(UnionTypeAttribute), true)
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInterfaceType(IClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsInterface
                || unresolvedType.Type.IsDefined(typeof(InterfaceTypeAttribute), true))
                && (unresolvedType.Context == TypeContext.Output
                    || unresolvedType.Context == TypeContext.None);
        }

        private static bool IsInputObjectType(IClrTypeReference unresolvedType)
        {
            return (IsComplexType(unresolvedType)
                || unresolvedType.Type.IsDefined(typeof(InputObjectTypeAttribute), true))
                && !unresolvedType.Type.IsAbstract
                && unresolvedType.Context == TypeContext.Input;
        }

        private static bool IsEnumType(IClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsEnum
                || unresolvedType.Type.IsDefined(typeof(EnumTypeAttribute), true))
                && IsPublic(unresolvedType);
        }

        private static bool IsComplexType(IClrTypeReference unresolvedType)
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

        private static bool IsPublic(IClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsPublic
                || unresolvedType.Type.IsNestedPublic;
        }
    }
}
