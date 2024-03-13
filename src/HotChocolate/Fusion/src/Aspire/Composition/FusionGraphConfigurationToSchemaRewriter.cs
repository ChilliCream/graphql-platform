using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Composition.Tooling;

internal sealed class FusionGraphConfigurationToSchemaRewriter : SyntaxRewriter<FusionTypeNames>
{
    public DocumentNode Rewrite(DocumentNode fusionGraph)
    {
        var typeNames = FusionTypeNames.From(fusionGraph);
        var schemaDoc = (DocumentNode?)Rewrite(fusionGraph, typeNames);

        if (schemaDoc is null)
        {
            throw new InvalidOperationException();
        }

        return schemaDoc;
    }

    protected override DirectiveNode? RewriteDirective(DirectiveNode node, FusionTypeNames context)
        => context.IsFusionDirective(node.Name.Value) ? null : base.RewriteDirective(node, context);
}
