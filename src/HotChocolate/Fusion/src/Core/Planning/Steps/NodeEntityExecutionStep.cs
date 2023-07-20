using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeEntityExecutionStep : ExecutionStep
{
    public NodeEntityExecutionStep(
        IObjectType entityType,
        ObjectTypeMetadata entityTypeMetadata,
        SelectionExecutionStep selectEntityStep)
        : base(null, entityType, entityTypeMetadata)
    {
        SelectEntityStep = selectEntityStep;
    }

    /// <summary>
    /// Gets the name of the entity type.
    /// </summary>
    public string TypeName => SelectionSetTypeMetadata.Name;

    /// <summary>
    /// Gets the data selection step.
    /// </summary>
    public SelectionExecutionStep SelectEntityStep { get; }
}
