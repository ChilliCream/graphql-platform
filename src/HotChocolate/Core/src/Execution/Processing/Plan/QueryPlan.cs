using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class QueryPlan
    {
        public QueryPlan(QueryPlanStep root)
        {
            Root = root;

            var count = 0;
            AssignId(root, ref count);
            Count = count;
        }

        public QueryPlanStep Root { get; }

        public int Count { get; }

        internal bool TryGetStep(
            IExecutionTask executionTask,
            [MaybeNullWhen(false)] out QueryPlanStep step) =>
            Root.TryGetStep(executionTask, out step);

        private static void AssignId(QueryPlanStep node, ref int stepId)
        {
            node.Id = stepId++;

            for (var i = 0; i < node.Steps.Count; i++)
            {
                AssignId(node.Steps[i], ref stepId);
            }
        }
    }
}
