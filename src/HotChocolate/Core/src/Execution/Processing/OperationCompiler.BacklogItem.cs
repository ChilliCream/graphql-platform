using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private readonly struct BacklogItem
    {
        public BacklogItem(
            IObjectType type,
            int selectionSetId,
            Selection selection,
            IImmutableList<ISelectionOptimizer> optimizers)
        {
            Type = type;
            SelectionSetId = selectionSetId;
            Selection = selection;
            Optimizers = optimizers;
        }

        public IObjectType Type { get; }

        public int SelectionSetId { get; }

        public Selection Selection { get; }

        public IImmutableList<ISelectionOptimizer> Optimizers { get; }
    }
}
