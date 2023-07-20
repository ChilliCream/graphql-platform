using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a execution step within the execution plan while being in the planing phase.
/// After the planing phase execution steps are compiled into execution nodes.
/// </summary>
internal abstract class ExecutionStep
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionStep"/>.
    /// </summary>
    /// <param name="parentSelection">
    /// The parent selection of this execution step.
    /// </param>
    /// <param name="selectionSetType">
    /// The declaring type of the selection set of this execution step.
    /// </param>
    /// <param name="selectionSetTypeMetadata">
    /// The declaring type of the selection set of this execution step.
    /// </param>
    protected ExecutionStep(
        ISelection? parentSelection,
        IObjectType selectionSetType,
        ObjectTypeMetadata selectionSetTypeMetadata)
    {
        ParentSelection = parentSelection;
        SelectionSetType = selectionSetType  ??
            throw new ArgumentNullException(nameof(selectionSetType));
        SelectionSetTypeMetadata = selectionSetTypeMetadata ??
            throw new ArgumentNullException(nameof(selectionSetTypeMetadata));
    }

    /// <summary>
    /// Gets the parent selection.
    /// </summary>
    public ISelection? ParentSelection { get; }

    /// <summary>
    /// Gets the declaring type of the selection set of this execution step.
    /// </summary>
    public ObjectTypeMetadata SelectionSetTypeMetadata { get; }

    /// <summary>
    /// Gets the declaring type of the selection set of this execution step.
    /// </summary>
    public IObjectType SelectionSetType { get; }

    /// <summary>
    /// Gets the execution steps this execution step is depending on.
    /// </summary>
    public HashSet<ExecutionStep> DependsOn { get; } = new();
}
