using System.Linq;

namespace HotChocolate.Execution.Processing.Plan;

internal sealed class ParallelStep : ExecutionStep
{
    public ParallelStep(ExecutionStep[] steps) : base(steps)
    {
    }

    public override string Name => "Parallel";

    public override string ToString()
    {
        return $"{Name}[{string.Join(", ", Steps.Select(t => t.Name))}]";
    }
}
