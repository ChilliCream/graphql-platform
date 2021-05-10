using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class QueryStepBatch
    {
        public QueryStepBatch(ExecutionStrategy strategy)
        {
            Strategy = strategy;
        }

        public ExecutionStrategy Strategy { get; }

        public List<QueryPlanStep> Steps { get; } = new();

        public List<SelectionBatch> Selections { get; } = new();
    }
}
