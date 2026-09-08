using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class FusionOperationInfo : RequestFeature
{
    public string? OperationId { get; set; }

    public OperationPlan? OperationPlan { get; set; }

    public DocumentNode? NormalizedDocument { get; set; }

    public OperationDefinitionNode? NormalizedOperation { get; set; }

    protected internal override void Reset()
    {
        OperationId = null;
        OperationPlan = null;
        NormalizedDocument = null;
        NormalizedOperation = null;
    }
}
