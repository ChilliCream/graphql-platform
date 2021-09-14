using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Properties;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class QueryPlan
    {
        private readonly QueryPlan[] _deferredPlans;
        private readonly Dictionary<int, QueryPlan>? _streamPlans;

        public QueryPlan(
            QueryPlanStep root,
            QueryPlan[]? deferredPlans = null,
            Dictionary<int, QueryPlan>? streamPlans = null)
        {
            Root = root;
            _deferredPlans = deferredPlans ?? Array.Empty<QueryPlan>();
            _streamPlans = streamPlans;

            var count = 0;
            AssignId(root, ref count);
            Count = count;
        }

        public QueryPlanStep Root { get; }

        public int Count { get; }

        public QueryPlan GetDeferredPlan(int fragmentId)
        {
            if (fragmentId >= _deferredPlans.Length)
            {
                throw new ArgumentException(
                    Resources.QueryPlan_InvalidFragmentId,
                    nameof(fragmentId));
            }

            return _deferredPlans[fragmentId];
        }

        public QueryPlan GetStreamPlan(int selectionId)
        {
            if (_streamPlans is null)
            {
                throw new NotSupportedException("This query plan has no streams.");
            }

            return _streamPlans[selectionId];
        }

        internal bool TryGetStep(
            IExecutionTask executionTask,
            [MaybeNullWhen(false)] out QueryPlanStep step) =>
            Root.TryGetStep(executionTask, out step);

        internal bool TryGetStep(
            int  stepId,
            [MaybeNullWhen(false)] out QueryPlanStep step) =>
            Root.TryGetStep(stepId, out step);

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
