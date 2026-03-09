using HotChocolate.Execution.Processing;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Projections.Optimizers;

internal sealed class NodeSelectionSetOptimizer(ISelectionSetOptimizer optimizer) : ISelectionSetOptimizer
{
    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        if (context.TypeContext.Features.TryGet<NodeTypeFeature>(out var feature)
            && feature.NodeResolver is { } nodeResolverInfo
            && nodeResolverInfo.QueryField?.HasProjectionMiddleware() == true)
        {
            optimizer.OptimizeSelectionSet(context);
        }
    }
}
