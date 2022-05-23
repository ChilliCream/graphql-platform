using HotChocolate.Language;
using HotChocolate.Stitching.Types.Directives;

namespace HotChocolate.Stitching.Types.Renaming;

public class CollectTypeRenameDirective<TContext>
    : CollectDirectiveVisitor<TContext>
    where TContext : IRewriteContext
{
    protected override bool ShouldCollect(ISyntaxNode node, TContext context, out ISyntaxNode? collectedNode)
    {
        RenameDirective? renameDirective = default;
        var result = Parent is ComplexTypeDefinitionNodeBase
                     && RenameDirective.TryParse(node, out renameDirective);

        collectedNode = renameDirective;
        return result;
    }

    public void Visit(TContext context)
    {
        Visit(context.Document, context);
    }
}
