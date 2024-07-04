using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FusionGraphConfigurationToSchemaRewriter
    : SyntaxRewriter<FusionGraphConfigurationToSchemaRewriter.Context>
{
    public DocumentNode Rewrite(DocumentNode fusionGraph)
    {
        var typeNames = FusionTypeNames.From(fusionGraph);
        var schemaDoc = (DocumentNode?)Rewrite(fusionGraph, new Context(typeNames));

        if (schemaDoc is null)
        {
            // This should not happen as we have already validated the fusion graph configuration.
            throw new InvalidOperationException(
                FusionRequestExecutorBuilderExtensions_AddFusionGatewayServer_NoSchema);
        }

        return schemaDoc;
    }

    protected override DirectiveNode? RewriteDirective(DirectiveNode node, Context context)
    {
        if (context.TypeNames.IsFusionDirective(node.Name.Value))
        {
            return null;
        }

        return base.RewriteDirective(node, context);
    }

    internal sealed class Context(FusionTypeNames typeNames)
    {
        public FusionTypeNames TypeNames { get; } = typeNames;
    }
}
