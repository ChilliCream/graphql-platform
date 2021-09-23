using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class SequenceStep : ExecutionStep
    {
        public SequenceStep(ExecutionStep[] steps, bool cancelOnError = false)
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
