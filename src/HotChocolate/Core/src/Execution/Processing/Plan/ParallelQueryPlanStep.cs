using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ParallelQueryPlanStep : ExecutionStep
    {
        public ParallelQueryPlanStep(ExecutionStep[] steps) : base(steps)
        {
        }

        public override string Name => "Parallel";

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", Steps.Select(t => t.Name))}]";
        }
    }
}
