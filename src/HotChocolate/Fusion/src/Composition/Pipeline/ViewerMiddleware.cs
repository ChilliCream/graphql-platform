using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ViewerMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var fusionGraph = context.FusionGraph;
        var fusionTypes = context.FusionTypes;

        if (fusionGraph.QueryType is not null &&
            fusionGraph.QueryType.Fields.TryGetField("viewer", out var viewerField) &&
            viewerField.Type.NamedType() is ObjectTypeDefinition { Name: "Viewer" } viewerType)
        {
            var viewerSelectionNode = new SelectionSetNode([new FieldNode("viewer")]);

            foreach (var subgraphSchema in context.Subgraphs)
            {
                if (subgraphSchema.QueryType is not null &&
                    subgraphSchema.QueryType.Fields.TryGetField("viewer", out var subgraphViewerField) &&
                    subgraphViewerField.Type.NamedType() is ObjectTypeDefinition { Name: "Viewer" })
                {
                    viewerType.Directives.Add(fusionTypes.CreateResolverDirective(subgraphSchema.Name, viewerSelectionNode));
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
