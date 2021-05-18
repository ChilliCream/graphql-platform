using System;
using System.Linq;
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

            OperationQueryPlanNode operationNode = Prepare(new QueryPlanContext(operation));

            if (operationNode.Deferred.Count == 0)
            {
                return new QueryPlan(operationNode.CreateStep());
            }

            return new QueryPlan(
                operationNode.CreateStep(),
                operationNode.Deferred.Select(t => new QueryPlan(t.CreateStep())).ToArray());
        }

        public static QueryPlanNode Prepare(IPreparedOperation operation)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return Prepare(new QueryPlanContext(operation));
        }

        private static OperationQueryPlanNode Prepare(QueryPlanContext context)
        {
            if (context.Operation.Definition.Operation is OperationType.Mutation)
            {
                return MutationStrategy.Build(context);
            }

            return QueryStrategy.Build(context);
        }

        internal static ExecutionStrategy GetStrategyFromSelection(ISelection selection) =>
            selection.Strategy == SelectionExecutionStrategy.Serial
                ? ExecutionStrategy.Serial
                : ExecutionStrategy.Parallel;
    }
}
