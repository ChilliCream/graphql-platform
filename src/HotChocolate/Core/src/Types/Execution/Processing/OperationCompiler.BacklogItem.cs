using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private readonly struct BacklogItem(
        ObjectType type,
        int selectionSetId,
        Selection selection,
        SelectionPath path,
        ImmutableArray<ISelectionSetOptimizer> optimizers)
    {
        public ObjectType Type { get; } = type;

        public int SelectionSetId { get; } = selectionSetId;

        public Selection Selection { get; } = selection;

        public SelectionPath Path { get; } = path;

        public ImmutableArray<ISelectionSetOptimizer> Optimizers { get; } = optimizers;
    }
}
