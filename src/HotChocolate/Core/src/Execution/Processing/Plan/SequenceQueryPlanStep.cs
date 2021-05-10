using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceQueryPlanStep : QueryPlanStep
    {
        private readonly QueryPlanStep[] _steps;

        public SequenceQueryPlanStep(QueryPlanStep[] steps)
        {
            Debug.Assert(steps.Length > 0, "Sequence cannot be empty.");

            _steps = steps;

            foreach (QueryPlanStep step in steps)
            {
                step.Parent = this;
            }
        }

        public override ExecutionStrategy Strategy => ExecutionStrategy.Serial;

        internal override IReadOnlyList<QueryPlanStep> Steps => _steps;

        internal QueryPlanStep? GetNextStep(QueryPlanStep current)
        {
            var index = Array.IndexOf(_steps, current);
            return index == -1 ? null : _steps[index];
        }
    }
}
