using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public static class TypeReferenceExtensions
    {
        public static ITypeReference With(
            this ITypeReference typeReference,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default,
            Optional<bool[]?> nullable = default)
        {
            switch (typeReference)
            {
                case ClrTypeReference clr:
                    return clr.With(context: context, scope: scope, nullable: nullable);
                case SchemaTypeReference schema:
                    return schema.With(context: context, scope: scope, nullable: nullable);
                case SyntaxTypeReference syntax:
                    return syntax.With(context: context, scope: scope, nullable: nullable);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
