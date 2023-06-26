using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeEntityExecutionStep : ExecutionStep
{
    public NodeEntityExecutionStep(
        IObjectType entityType,
        ObjectTypeInfo entityTypeInfo,
        SelectionExecutionStep selectEntityStep)
        : base(null, entityType, entityTypeInfo)
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
