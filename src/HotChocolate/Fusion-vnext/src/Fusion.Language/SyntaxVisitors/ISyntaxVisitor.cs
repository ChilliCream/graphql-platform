namespace HotChocolate.Fusion;

internal interface ISyntaxVisitor<in TContext>
{
    ISyntaxVisitorAction Visit(IFieldSelectionMapSyntaxNode node, TContext context);
}
