using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing;

// TODO : We might remove this
internal sealed class OperationCompilerOptimizers
{
    private ImmutableArray<IOperationOptimizer> _operationOptimizers = [];
    private ImmutableArray<ISelectionSetOptimizer> _selectionSetOptimizers = [];
    private PropertyInitFlags _initFlags;

    public ImmutableArray<IOperationOptimizer> OperationOptimizers
    {
        get => _operationOptimizers;
        set
        {
            if ((_initFlags & PropertyInitFlags.OperationOptimizers) == PropertyInitFlags.OperationOptimizers)
            {
                throw new InvalidOperationException(
                    "OperationOptimizers can only be set once.");
            }

            _initFlags |= PropertyInitFlags.OperationOptimizers;
            _operationOptimizers = value;
        }
    }

    public ImmutableArray<ISelectionSetOptimizer> SelectionSetOptimizers
    {
        get => _selectionSetOptimizers;
        set
        {
            if ((_initFlags & PropertyInitFlags.SelectionSetOptimizers) == PropertyInitFlags.SelectionSetOptimizers)
            {
                throw new InvalidOperationException(
                    "OperationOptimizers can only be set once.");
            }

            _initFlags |= PropertyInitFlags.SelectionSetOptimizers;
            _selectionSetOptimizers = value;
        }
    }

    [Flags]
    private enum PropertyInitFlags
    {
        OperationOptimizers = 1,
        SelectionSetOptimizers = 2
    }
}
