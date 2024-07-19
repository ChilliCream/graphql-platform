using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing;

internal sealed class OperationCompilerFeature
{
    public OperationCompilerFeature(IEnumerable<IOperationCompilerOptimizer> optimizers)
    {
        var operationOptimizers = ImmutableArray.CreateBuilder<IOperationOptimizer>();
        var selectionSetOptimizers = ImmutableArray.CreateBuilder<ISelectionSetOptimizer>();

        foreach (var optimizer in optimizers)
        {
            if (optimizer is IOperationOptimizer operationOptimizer)
            {
                operationOptimizers.Add(operationOptimizer);
            }

            if (optimizer is ISelectionSetOptimizer selectionSetOptimizer)
            {
                selectionSetOptimizers.Add(selectionSetOptimizer);
            }
        }

        OperationOptimizers = operationOptimizers.ToImmutable();
        SelectionSetOptimizers = selectionSetOptimizers.ToImmutable();
    }

    public ImmutableArray<IOperationOptimizer> OperationOptimizers { get; }

    public ImmutableArray<ISelectionSetOptimizer> SelectionSetOptimizers { get; }
}
