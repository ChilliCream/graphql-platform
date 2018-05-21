using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal static class SchemaContextExtensions
    {
        public static IOutputType GetOutputType(
            this SchemaContext context, ITypeNode type)
        {
            if (type.Kind == NodeKind.NonNullType)
            {
                return new NonNullType(
                    GetOutputType(context,
                        ((NonNullTypeNode)type).Type));
            }

            if (type.Kind == NodeKind.ListType)
            {
                return new ListType(GetOutputType(
                    context, ((ListTypeNode)type).Type));
            }

            if (type.Kind == NodeKind.NamedType)
            {
                return context.GetOutputType(((NamedTypeNode)type).Name.Value);
            }

            throw new NotSupportedException();
        }

        public static IInputType GetInputType(
            this SchemaContext context, ITypeNode type)
        {
            if (type.Kind == NodeKind.NonNullType)
            {
                return new NonNullType(
                    GetInputType(context,
                        ((NonNullTypeNode)type).Type));
            }

            if (type.Kind == NodeKind.ListType)
            {
                return new ListType(GetInputType(
                    context, ((ListTypeNode)type).Type));
            }

            if (type.Kind == NodeKind.NamedType)
            {
                return context.GetInputType(((NamedTypeNode)type).Name.Value);
            }

            throw new NotSupportedException();
        }

        public static ScalarType StringType(this ISchemaContext context)
        {
            return context.GetOutputType<StringType>(WellKnownTypes.StringType);
        }

        public static NonNullType NonNullStringType(this ISchemaContext context)
        {
            return new NonNullType(context.StringType());
        }

        public static ScalarType BooleanType(this ISchemaContext context)
        {
            // TODO : boolean type
            return context.GetOutputType<ScalarType>(WellKnownTypes.BooleanType);
        }

        public static NonNullType NonNullBooleanType(this ISchemaContext context)
        {
            return new NonNullType(context.BooleanType());
        }
    }
}
