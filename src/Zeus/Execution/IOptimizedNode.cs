using System.Collections.Generic;

namespace Zeus.Execution
{
    internal interface IOptimizedNode
    {
        IOptimizedNode Parent { get; }

        IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        IOptimizedNode AddSelections(IEnumerable<IOptimizedSelection> selections);

        IOptimizedNode ReplaceSelection(IOptimizedSelection oldSelection, IOptimizedSelection newSelection);
    }
}