using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeEntityExecutionStep(
    int id,
    IObjectType entityType,
    ObjectTypeMetadata entityTypeMetadata,
    SelectionExecutionStep selectEntityStep)
    : ExecutionStep(id, null, entityType, entityTypeMetadata)
{
    /// <summary>
    /// Gets the name of the entity type.
    /// </summary>
    public string TypeName => SelectionSetTypeMetadata.Name;

    /// <summary>
    /// Gets the data selection step.
    /// </summary>
    public SelectionExecutionStep SelectEntityStep { get; } = selectEntityStep;
}
