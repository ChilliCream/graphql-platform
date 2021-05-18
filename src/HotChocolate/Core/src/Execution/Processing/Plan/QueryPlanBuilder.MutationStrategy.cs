using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal static partial class QueryPlanBuilder
    {
        private static class MutationStrategy
        {
            public static OperationQueryPlanNode Build(QueryPlanContext context)
            {
                var root = new SequenceQueryPlanNode();

                context.Root = root;
                context.NodePath.Push(root);

                foreach (ISelection mutation in context.Operation.GetRootSelectionSet().Selections)
                {
                    context.SelectionPath.Push(mutation);

                    var mutationStep = new ResolverQueryPlanNode(
                        mutation,
                        context.SelectionPath.PeekOrDefault(),
                        ExecutionStrategy.Serial);

                    root.AddNode(mutationStep);
                    context.NodePath.Push(mutationStep);

                    QueryStrategy.VisitChildren(mutation, context);

                    context.NodePath.Pop();
                }

                context.NodePath.Pop();

                QueryPlanNode optimized = QueryStrategy.Optimize(context.Root);
                var operationNode = new OperationQueryPlanNode(optimized);

                if (context.Deferred.Count > 0)
                {
                    foreach (var deferred in QueryStrategy.BuildDeferred(context))
                    {
                        operationNode.Deferred.Add(deferred);
                    }
                }

                return operationNode;
            }
        }
    }
}
