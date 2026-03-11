namespace HotChocolate.Types;

[DirectiveType(DirectiveNames.SemanticNonNull.Name, DirectiveLocation.FieldDefinition, IsRepeatable = false)]
public sealed class SemanticNonNullDirective(IReadOnlyList<int> levels)
{
    [GraphQLType<ListType<NonNullType<IntType>>>]
    [DefaultValueSyntax("[0]")]
    public IReadOnlyList<int>? Levels { get; } = levels;
}
