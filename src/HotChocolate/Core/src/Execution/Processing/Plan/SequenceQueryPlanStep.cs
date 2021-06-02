using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        protected internal override string Name => "Sequence";

        internal override IReadOnlyList<QueryPlanStep> Steps => _steps;

        internal QueryPlanStep? GetNextStep(QueryPlanStep current)
        {
            var index = Array.IndexOf(_steps, current);

            if (index == -1)
            {
                return null;
            }

            // move index to the next item.
            index++;

            if (index < _steps.Length)
            {
                return _steps[index];
            }

            return null;
        }

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", _steps.Select(t => t.Name))}]";
        }
    }
}
