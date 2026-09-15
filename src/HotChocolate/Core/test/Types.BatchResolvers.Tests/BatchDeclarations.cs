namespace HotChocolate.Types.BatchResolvers;

public sealed record BatchDeclarations
{
    public required Declaration Attribute { get; init; }

    public required Declaration SourceGenerated { get; init; }

    public required Declaration Fluent { get; init; }

    public Declaration this[DeclarationStyle style] => style switch
    {
        DeclarationStyle.Attribute => Attribute,
        DeclarationStyle.SourceGenerated => SourceGenerated,
        DeclarationStyle.Fluent => Fluent,
        _ => throw ThrowHelper.UnknownDeclarationStyle(style)
    };
}
