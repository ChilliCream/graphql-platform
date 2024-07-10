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
        IImmutableList<ISelectionSetOptimizer> optimizers)
    {
        public ObjectType Type { get; } = type;

        public int SelectionSetId { get; } = selectionSetId;

        public Selection Selection { get; } = selection;

        public SelectionPath Path { get; } = path;

        public IImmutableList<ISelectionSetOptimizer> Optimizers { get; } = optimizers;
    }
}
