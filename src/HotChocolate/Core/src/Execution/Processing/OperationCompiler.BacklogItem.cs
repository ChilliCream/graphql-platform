using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private readonly struct BacklogItem
    {
        public BacklogItem(
            ObjectType type,
            int selectionSetId,
            Selection selection,
            SelectionPath path,
            IImmutableList<ISelectionSetOptimizer> optimizers)
        {
            Type = type;
            SelectionSetId = selectionSetId;
            Selection = selection;
            Path = path;
            Optimizers = optimizers;
        }

        public ObjectType Type { get; }

        public int SelectionSetId { get; }

        public Selection Selection { get; }

        public SelectionPath Path { get; }

        public IImmutableList<ISelectionSetOptimizer> Optimizers { get; }
    }
}
