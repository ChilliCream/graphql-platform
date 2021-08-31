using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class QueryPlanBuilderContext
    {
        public QueryPlanBuilderContext(IPreparedOperation operation)
        {
            Operation = operation;
            Batches.Add(new QueryStepBatch(ExecutionStrategy.Serial));
        }

        public IPreparedOperation Operation { get; }

        public List<QueryStepBatch> Batches { get; } = new();

        public List<ISelection> Path { get; } = new();
    }
}
