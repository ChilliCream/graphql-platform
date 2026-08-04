namespace HotChocolate.Fusion.Language;

internal interface ISyntaxVisitor<in TContext>
{
    ISyntaxVisitorAction Visit(IFieldSelectionMapSyntaxNode node, TContext context);
}
