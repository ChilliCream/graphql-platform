using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public static class TypeReferenceExtensions
    {
        public static ITypeReference With(
            this ITypeReference typeReference,
            Optional<TypeContext> context = default,
            Optional<string?> scope = default) => typeReference switch
            {
                ExtendedTypeReference clr => clr.With(context: context, scope: scope),
                SchemaTypeReference schema => schema.With(context: context, scope: scope),
                SyntaxTypeReference syntax => syntax.With(context: context, scope: scope),
                _ => throw new NotSupportedException()
            };
    }
}
