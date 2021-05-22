using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan
{
    internal readonly struct SelectionBatch
    {
        public SelectionBatch(ExecutionStrategy strategy)
        {
            Strategy = strategy;
            Selections = new HashSet<uint>();
        }

        public ExecutionStrategy Strategy { get; }

        public HashSet<uint> Selections { get; }
    }
}
