using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class NodeMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var fusionGraph = context.FusionGraph;
        var fusionTypes = context.FusionTypes;

        if (fusionGraph.QueryType is not null &&
            context.Features.IsNodeFieldSupported() &&
            fusionGraph.QueryType.Fields.TryGetField("node", out var nodeField))
        {
            fusionGraph.QueryType.Fields.TryGetField("nodes", out var nodesField);

            var nodes = new HashSet<ObjectTypeDefinition>();

            foreach (var schema in context.Subgraphs)
            {
                nodes.Clear();

                if (schema.Types.TryGetType<InterfaceTypeDefinition>("Node", out var nodeInterface))
                {
                    foreach (var possibleNode in schema.Types.OfType<ObjectTypeDefinition>())
                    {
                        if (possibleNode.Implements.Contains(nodeInterface))
                        {
                            var nodeName = possibleNode.Name;
                            nodes.Add(possibleNode);

                            if (possibleNode.TryGetOriginalName(out var originalNodeName) &&
                                !originalNodeName.EqualsOrdinal(nodeName) &&
                                fusionGraph.Types.TryGetType<ObjectTypeDefinition>(nodeName, out var node) &&
                                node.Fields.TryGetField("id", out var idField) &&
                                !node.Directives.ContainsName(fusionTypes.ReEncodeId.Name))
                            {
                                idField.Directives.Add(fusionTypes.CreateReEncodeIdDirective());
                            }
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
