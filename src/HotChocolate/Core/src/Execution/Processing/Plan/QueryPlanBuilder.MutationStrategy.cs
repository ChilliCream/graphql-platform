using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal static partial class QueryPlanBuilder
    {
        private static class MutationStrategy
        {
            public static QueryPlanNode Build(IPreparedOperation operation)
            {
                var root = new SequenceQueryPlanNode();

                var context = new QueryPlanContext(operation);
                context.Root = root;
                context.NodePath.Push(root);

                foreach (ISelection mutation in operation.GetRootSelectionSet().Selections)
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

                return QueryStrategy.Optimize(context.Root);
            }
        }
    }
}
