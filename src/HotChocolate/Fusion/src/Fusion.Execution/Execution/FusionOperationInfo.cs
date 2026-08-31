using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionOperationInfo : RequestFeature
{
    public string? OperationId { get; set; }

    public OperationPlan? OperationPlan { get; set; }

    public ImmutableArray<IPolicy> PolicySnapshot { get; set; }

    protected internal override void Reset()
    {
        OperationId = null;
        OperationPlan = null;
        PolicySnapshot = default;
    }
}
