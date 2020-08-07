using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public static class FilterFieldDescriptorExtensions
    {
        public static void IsNullable(this IFilterFieldDescriptor descriptor) =>
            descriptor.Extend().OnBeforeCreate(def => def.Type = RewriteTypeToNullableType(def));

        public static void IsNullable(this IFilterOperationFieldDescriptor descriptor) =>
            descriptor.Extend().OnBeforeCreate(def => def.Type = RewriteTypeToNullableType(def));

        private static ITypeReference RewriteTypeToNullableType(FilterFieldDefinition definition)
        {
            ITypeReference reference = definition.Type;

            if (reference is ClrTypeReference clrRef
                && TypeInspector.Default.TryCreate(
                    clrRef.Type,
                    out TypeInfo typeInfo))
            {
                if (BaseTypes.IsSchemaType(typeInfo.ClrType))
                {
                    if (clrRef.Type.IsGenericType
                        && clrRef.Type.GetGenericTypeDefinition() ==
                            typeof(NonNullType<>))
                    {
                        return clrRef.WithType(typeInfo.Components[1]);
                    }
                    return clrRef;
                }
                else
                {
                    if (clrRef.Type.IsValueType)
                    {
                        if (System.Nullable.GetUnderlyingType(clrRef.Type) == null)
                        {
                            return clrRef.WithType(
                                typeof(Nullable<>).MakeGenericType(clrRef.Type));
                        }
                        return clrRef;
                    }
                    else if (clrRef.Type.IsGenericType
                        && clrRef.Type.GetGenericTypeDefinition() ==
                            typeof(NonNullType<>))
                    {
                        return clrRef.WithType(typeInfo.Components[1]);
                    }
                    return clrRef;
                }
            }

            if (reference is SchemaTypeReference schemaRef)
            {
                return schemaRef.Type is NonNullType nnt
                    ? schemaRef.WithType(nnt.Type)
                    : schemaRef;
            }

            if (reference is SyntaxTypeReference syntaxRef)
            {
                return syntaxRef.Type is NonNullTypeNode nnt
                    ? syntaxRef.WithType(nnt.Type)
                    : syntaxRef;
            }

            throw new NotSupportedException();
        }
    }
}
