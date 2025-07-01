using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionOperationInfo : RequestFeature
{
    public string? OperationId { get; set; }

    public OperationExecutionPlan? OperationPlan { get; set; }

    protected override void Reset()
    {
        OperationId = null;
        OperationPlan = null;
    }
}
