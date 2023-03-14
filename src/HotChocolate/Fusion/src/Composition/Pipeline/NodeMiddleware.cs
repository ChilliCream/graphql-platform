using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class NodeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        if (context.FusionGraph.QueryType is not null &&
            (context.Features & FusionFeatureFlags.NodeField) == FusionFeatureFlags.NodeField &&
            context.FusionGraph.QueryType.Fields.TryGetField("node", out var nodeField))
        {
            context.FusionGraph.QueryType.Fields.TryGetField("nodes", out var nodesField);

            var nodes = new HashSet<ObjectType>();

            foreach (var schema in context.Subgraphs)
            {
                nodes.Clear();

                if (schema.Types.TryGetType<InterfaceType>("Node", out var nodeInterface))
                {
                    foreach (var objectType in schema.Types.OfType<ObjectType>())
                    {
                        if (objectType.Implements.Contains(nodeInterface))
                        {
                            nodes.Add(objectType);
                        }
                    }
                }

                if (nodes.Count > 0)
                {
                    var nodeDirective = context.FusionTypes.CreateNodeDirective(schema.Name, nodes);
                    nodeField.Directives.Add(nodeDirective);
                    nodesField?.Directives.Add(nodeDirective);
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
