namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeUnionTypeCompletionContext(
    FusionDirective[] directives,
    FusionObjectTypeDefinition[] types)
{
    public FusionDirective[] Directives { get; } = directives;

    public FusionObjectTypeDefinition[] Types { get; } = types;
}
