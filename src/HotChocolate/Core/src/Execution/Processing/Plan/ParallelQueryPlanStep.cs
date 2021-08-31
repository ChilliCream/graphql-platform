using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ParallelQueryPlanStep : QueryPlanStep
    {
        private readonly QueryPlanStep[] _steps;

        public ParallelQueryPlanStep(QueryPlanStep[] steps)
        {
            Debug.Assert(steps.Length > 0, "Parallel cannot be empty.");

            _steps = steps;

            foreach (QueryPlanStep step in steps)
            {
                step.Parent = this;
            }
        }

        protected internal override string Name => "Parallel";

        internal override IReadOnlyList<QueryPlanStep> Steps => _steps;

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", _steps.Select(t => t.Name))}]";
        }
    }
}
