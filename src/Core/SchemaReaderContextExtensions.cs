using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal static class SchemaReaderContextExtensions
    {
        public static IOutputType GetOutputType(
            this SchemaReaderContext context,
            ITypeNode type)
        {
            if (type.Kind == NodeKind.NonNullType)
            {
                return GetOutputType(context, ((NonNullTypeNode)type).Type);
            }

            if (type.Kind == NodeKind.ListType)
            {
                return GetOutputType(context, ((ListTypeNode)type).Type);
            }

            if (type.Kind == NodeKind.NamedType)
            {
                return context.GetOutputType(((NamedTypeNode)type).Name.Value);
            }

            throw new NotSupportedException();
        }

        public static IInputType GetInputType(
            this SchemaReaderContext context,
            ITypeNode type)
        {
            if (type.Kind == NodeKind.NonNullType)
            {
                return GetInputType(context, ((NonNullTypeNode)type).Type);
            }

            if (type.Kind == NodeKind.ListType)
            {
                return GetInputType(context, ((ListTypeNode)type).Type);
            }

            if (type.Kind == NodeKind.NamedType)
            {
                return context.GetInputType(((NamedTypeNode)type).Name.Value);
            }

            throw new NotSupportedException();
        }
    }
}