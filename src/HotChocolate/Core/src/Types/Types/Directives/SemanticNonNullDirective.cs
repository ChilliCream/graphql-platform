#nullable enable

namespace HotChocolate.Types;

[DirectiveType(WellKnownDirectives.SemanticNonNull, DirectiveLocation.FieldDefinition, IsRepeatable = false)]
[GraphQLDescription(Description)]
public sealed class SemanticNonNullDirective
{
    private static readonly List<int> _defaultLevels = [0];

    public const string Description = "TODO";
    public const string LevelsDescription = "TODO";

    [GraphQLDescription(LevelsDescription)]
    [GraphQLType<ListType<NonNullType<IntType>>>]
    [DefaultValueSyntax("[0]")]
    public IReadOnlyList<int>? Levels { get; set; } = _defaultLevels;
}
