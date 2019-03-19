using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    internal static class SchemaTypeResolver
    {
        public static bool TryInferSchemaType(
            IClrTypeReference unresolvedType,
            out IClrTypeReference schemaType)
        {
            if (IsInterfaceType(unresolvedType))
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
                    TypeContext.Output);
            }
            else if (IsEnumType(unresolvedType))
            {
                schemaType = new ClrTypeReference(typeof(EnumType<>)
                    .MakeGenericType(unresolvedType.Type),
                    TypeContext.Output);
            }
            else
            {
                schemaType = null;
            }

            return schemaType != null;
        }

        private static bool IsObjectType(IClrTypeReference unresolvedType)
        {
            return IsComplexType(unresolvedType)
                && unresolvedType.Context == TypeContext.Output;
        }

        private static bool IsInterfaceType(IClrTypeReference unresolvedType)
        {
            return unresolvedType.Type.IsInterface
                && unresolvedType.Context == TypeContext.Output;
        }

        private static bool IsInputObjectType(IClrTypeReference unresolvedType)
        {
            return IsComplexType(unresolvedType)
                && unresolvedType.Context == TypeContext.Input;
        }

        private static bool IsComplexType(IClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsClass
                    && !unresolvedType.Type.IsAbstract
                    && (unresolvedType.Type.IsPublic
                        || unresolvedType.Type.IsNestedPublic))
                    && unresolvedType.Type != typeof(string);
        }

        private static bool IsEnumType(IClrTypeReference unresolvedType)
        {
            return (unresolvedType.Type.IsEnum
                    && (unresolvedType.Type.IsPublic
                        || unresolvedType.Type.IsNestedPublic));
        }
    }
}
