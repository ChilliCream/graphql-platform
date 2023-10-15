using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class NodeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var fusionGraph = context.FusionGraph;

        if (fusionGraph.QueryType is not null &&
            context.Features.IsNodeFieldSupported() &&
            fusionGraph.QueryType.Fields.TryGetField("node", out _))
        {
            fusionGraph.QueryType.Fields.TryGetField("nodes", out _);

            var nodes = new HashSet<ObjectType>();

            foreach (var schema in context.Subgraphs)
            {
                nodes.Clear();

                if (schema.Types.TryGetType<InterfaceType>("Node", out var nodeInterface))
                {
                    foreach (var possibleNode in schema.Types.OfType<ObjectType>())
                    {
                        if (possibleNode.Implements.Contains(nodeInterface))
                        {
                            nodes.Add(possibleNode);
                        }
                    }
                }

                if (nodes.Count > 0)
                {
                    var nodeDirective = context.FusionTypes.CreateNodeDirective(schema.Name, nodes);
                    context.FusionGraph.Directives.Add(nodeDirective);
                }
            }
        }

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }
}