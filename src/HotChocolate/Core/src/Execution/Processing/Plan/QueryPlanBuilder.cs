using System;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal static partial class QueryPlanBuilder
    {
        public static QueryPlan Build(IPreparedOperation operation)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return new(Prepare(operation).CreateStep());
        }

        public static QueryPlanNode Prepare(IPreparedOperation operation)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.Definition.Operation == OperationType.Mutation)
            {
                return MutationStrategy.Build(operation);
            }

            return QueryStrategy.Build(operation);
        }

        internal static ExecutionStrategy GetStrategyFromSelection(ISelection selection) =>
            selection.Strategy == SelectionExecutionStrategy.Serial
                ? ExecutionStrategy.Serial
                : ExecutionStrategy.Parallel;
    }
}
