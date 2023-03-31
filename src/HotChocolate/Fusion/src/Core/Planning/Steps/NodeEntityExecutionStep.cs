using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeEntityExecutionStep : ExecutionStep
{
    public NodeEntityExecutionStep(
        ObjectTypeInfo entityTypeInfo,
        SelectionExecutionStep selectEntityStep)
        : base(entityTypeInfo, null)
    {
        SelectEntityStep = selectEntityStep;
    }

    /// <summary>
    /// Gets the name of the entity type.
    /// </summary>
    public string TypeName => SelectionSetTypeInfo.Name;

    /// <summary>
    /// Gets the data selection step.
    /// </summary>
    public SelectionExecutionStep SelectEntityStep { get; }
}
