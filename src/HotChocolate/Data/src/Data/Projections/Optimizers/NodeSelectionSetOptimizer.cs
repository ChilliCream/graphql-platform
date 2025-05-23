using HotChocolate.Execution.Processing;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Projections;

internal sealed class NodeSelectionSetOptimizer(ISelectionSetOptimizer optimizer) : ISelectionSetOptimizer
{
    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        if(context.Type.Features.TryGet<NodeResolverInfo>(out var nodeResolverInfo)
            && nodeResolverInfo.QueryField?.HasProjectionMiddleware() == true)
        {
            optimizer.OptimizeSelectionSet(context);
        }
    }
}
