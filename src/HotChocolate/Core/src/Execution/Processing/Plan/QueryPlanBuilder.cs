using System;
using System.Collections.Generic;
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

            var context = new QueryPlanContext(operation);

            OperationQueryPlanNode operationNode = Prepare(context);
            QueryPlan[] deferredPlans = Array.Empty<QueryPlan>();
            Dictionary<int, QueryPlan>? streamPlans = null;

            if (operationNode.Deferred.Count > 0)
            {
                deferredPlans = new QueryPlan[operationNode.Deferred.Count];

                for (var i = 0; i < operationNode.Deferred.Count; i++)
                {
                    deferredPlans[i] = new QueryPlan(operationNode.Deferred[i].CreateStep());
                }
            }

            if (context.Streams.Count > 0)
            {
                streamPlans = new Dictionary<int, QueryPlan>();
                foreach (StreamPlanNode streamPlan in context.Streams)
                {
                    streamPlans.Add(streamPlan.Id, new QueryPlan(streamPlan.Root.CreateStep()));
                }
            }

            return new QueryPlan(operationNode.CreateStep(), deferredPlans, streamPlans);
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
            return context.Operation.Definition.Operation is OperationType.Mutation
                ? MutationStrategy.Build(context)
                : QueryStrategy.Build(context);
        }

        internal static ExecutionStrategy GetStrategyFromSelection(ISelection selection) =>
            selection.Strategy == SelectionExecutionStrategy.Serial
                ? ExecutionStrategy.Serial
                : ExecutionStrategy.Parallel;
    }
}
