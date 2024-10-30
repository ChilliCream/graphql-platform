#nullable enable

namespace HotChocolate.Types.Descriptors;

public static class TypeReferenceExtensions
{
    public static TypeReference With(
        this TypeReference typeReference,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default) => typeReference switch
    {
        ExtendedTypeReference clr => clr.With(context: context, scope: scope),
        SchemaTypeReference schema => schema.With(context: context, scope: scope),
        SyntaxTypeReference syntax => syntax.With(context: context, scope: scope),
        DependantFactoryTypeReference d => d.With(context: context, scope: scope),
        _ => throw new NotSupportedException(),
    };
}
