#nullable enable

namespace HotChocolate.Types;

[DirectiveType(WellKnownDirectives.SemanticNonNull, DirectiveLocation.FieldDefinition, IsRepeatable = false)]
public sealed class SemanticNonNullDirective(IReadOnlyList<int> levels)
{
#if NETSTANDARD2_0
    [GraphQLType(typeof(ListType<NonNullType<IntType>>))]
#else
    [GraphQLType<ListType<NonNullType<IntType>>>]
#endif
    [DefaultValueSyntax("[0]")]
    public IReadOnlyList<int>? Levels { get; } = levels;
}
