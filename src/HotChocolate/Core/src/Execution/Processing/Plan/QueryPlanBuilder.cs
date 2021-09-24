using System;
using System.Collections.Generic;
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

            OperationNode operationNode = Prepare(context);

            QueryPlan[] deferredPlans =
                operationNode.Deferred.Count > 0
                    ? new QueryPlan[operationNode.Deferred.Count]
                    : Array.Empty<QueryPlan>();

            Dictionary<int, QueryPlan>? streamPlans =
                context.Streams.Count > 0
                    ? new Dictionary<int, QueryPlan>()
                    : null;

            if (operationNode.Deferred.Count > 0)
            {
                for (var i = 0; i < operationNode.Deferred.Count; i++)
                {
                    deferredPlans[i] = new QueryPlan(
                        operationNode.Deferred[i].CreateStep(),
                        deferredPlans,
                        streamPlans);
                }
            }

            if (context.Streams.Count > 0)
            {
                foreach (StreamPlanNode streamPlanNode in context.Streams.Values)
                {
                    var streamPlan = new QueryPlan(
                        streamPlanNode.Root.CreateStep(),
                        deferredPlans,
                        streamPlans);

                    streamPlans!.Add(streamPlanNode.Id, streamPlan);
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

        private static OperationNode Prepare(QueryPlanContext context)
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
