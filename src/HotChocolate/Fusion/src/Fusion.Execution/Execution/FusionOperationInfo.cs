using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionOperationInfo : RequestFeature
{
    public OperationPlan? OperationPlan { get; set; }

    protected internal override void Reset()
    {
        OperationPlan = null;
    }
}
