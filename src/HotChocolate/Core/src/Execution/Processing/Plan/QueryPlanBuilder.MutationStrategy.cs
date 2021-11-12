using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan;

internal static partial class QueryPlanBuilder
{
    private static class MutationStrategy
    {
        public static OperationNode Build(QueryPlanContext context)
        {
            var root = new SequenceNode { CancelOnError = true };

            context.Root = root;
            context.NodePath.Push(root);

            foreach (ISelection mutation in context.Operation.GetRootSelectionSet().Selections)
            {
                context.SelectionPath.Push(mutation);

                var mutationStep = new ResolverNode(
                    mutation,
                    context.SelectionPath.PeekOrDefault(),
                    GetStrategyFromSelection(mutation));

                root.AddNode(mutationStep);

                QueryStrategy.VisitChildren(mutation, context);
            }

            context.NodePath.Pop();

            QueryPlanNode optimized = QueryStrategy.Optimize(context.Root);
            var operationNode = new OperationNode(optimized);

            if (context.Deferred.Count > 0)
            {
                foreach (QueryPlanNode? deferred in QueryStrategy.BuildDeferred(context))
                {
                    operationNode.Deferred.Add(deferred);
                }
            }

            if (context.Streams.Count > 0)
            {
                operationNode.Streams.AddRange(context.Streams.Values);
            }

            return operationNode;
        }
    }
}
