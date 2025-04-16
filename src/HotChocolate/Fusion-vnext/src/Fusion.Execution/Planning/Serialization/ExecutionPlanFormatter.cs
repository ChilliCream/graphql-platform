using System.Text;

namespace HotChocolate.Fusion.Planning;

public abstract class ExecutionPlanFormatter
{
    public abstract string Format(ExecutionPlan plan);
}
