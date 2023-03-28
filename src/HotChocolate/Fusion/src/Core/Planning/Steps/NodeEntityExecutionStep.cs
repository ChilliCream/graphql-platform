using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeEntityExecutionStep : ExecutionStep
{
    public NodeEntityExecutionStep(
        ObjectTypeInfo entityTypeInfo,
        SelectionExecutionStep entitySelectionExecutionStep)
        : base(entityTypeInfo, null)
    {
        EntitySelectionExecutionStep = entitySelectionExecutionStep;
    }

    public SelectionExecutionStep EntitySelectionExecutionStep { get; }
}
