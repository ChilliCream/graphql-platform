using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceQueryPlanStep : ExecutionStep
    {
        public SequenceQueryPlanStep(ExecutionStep[] steps, bool cancelOnError)
            : base(steps)
        {
            Debug.Assert(steps.Length > 0, "Sequence cannot be empty.");
            CancelOnError = cancelOnError;
        }

        public override string Name => "Sequence";

        public bool CancelOnError { get; }

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", Steps.Select(t => t.Name))}]";
        }
    }
}
