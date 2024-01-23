using HotChocolate.Execution.Processing;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Projections;

internal sealed class NodeSelectionSetOptimizer : ISelectionSetOptimizer
{
    private readonly ISelectionSetOptimizer _optimizer;

    public NodeSelectionSetOptimizer(ISelectionSetOptimizer optimizer)
    {
        _optimizer = optimizer;
    }

    public void OptimizeSelectionSet(SelectionSetOptimizerContext context)
    {
        if (context.Type.ContextData.TryGetValue(WellKnownContextData.NodeResolver, out var o) &&
            o is NodeResolverInfo { QueryField.ContextData: var fieldContextData, } &&
           fieldContextData.ContainsKey(ProjectionProvider.ProjectionContextIdentifier))
        {
            _optimizer.OptimizeSelectionSet(context);
        }
    }
}
