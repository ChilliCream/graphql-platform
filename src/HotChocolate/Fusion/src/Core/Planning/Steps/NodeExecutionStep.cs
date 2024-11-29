using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a execution step for a node field within
/// the execution plan while being in the planing phase.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="NodeExecutionStep"/>.
/// </remarks>
/// <param name="id">
/// The id of the execution step.
/// </param>
/// <param name="nodeSelection">
/// The node selection.
/// </param>
/// <param name="queryType">
/// The query type.
/// </param>
/// <param name="queryTypeMetadata">
/// The query type metadata.
/// </param>
/// <exception cref="ArgumentNullException">
/// <paramref name="nodeSelection"/> is <c>null</c>.
/// </exception>
internal sealed class NodeExecutionStep(
    int id,
    ISelection nodeSelection,
    IObjectType queryType,
    ObjectTypeMetadata queryTypeMetadata)
    : ExecutionStep(id, null, queryType, queryTypeMetadata)
{
    /// <summary>
    /// Gets the nodes selection.
    /// </summary>
    public ISelection NodeSelection { get; } = nodeSelection
        ?? throw new ArgumentNullException(nameof(nodeSelection));

    /// <summary>
    /// Gets the execution steps that handle the various entity types.
    /// </summary>
    public List<NodeEntityExecutionStep> EntitySteps { get; } = [];
}
