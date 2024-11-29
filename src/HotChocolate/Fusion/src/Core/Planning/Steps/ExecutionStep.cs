using System.Diagnostics;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a execution step within the execution plan while being in the planing phase.
/// After the planing phase execution steps are compiled into execution nodes.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal abstract class ExecutionStep
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionStep"/>.
    /// </summary>
    /// <param name="id">
    /// The query plan unique id of this execution step.
    /// </param>
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
        int id,
        ISelection? parentSelection,
        IObjectType selectionSetType,
        ObjectTypeMetadata selectionSetTypeMetadata)
    {
        ArgumentNullException.ThrowIfNull(selectionSetType);
        ArgumentNullException.ThrowIfNull(selectionSetTypeMetadata);
        Id = id;
        ParentSelection = parentSelection;
        SelectionSetType = selectionSetType;
        SelectionSetTypeMetadata = selectionSetTypeMetadata;
    }

    /// <summary>
    /// Gets the id of the execution step.
    /// </summary>
    public int Id { get; }

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
    public HashSet<ExecutionStep> DependsOn { get; } = [];

    private string GetDebuggerDisplay()
    {
        var displayName = $"{Id} {SelectionSetType.Name}";

        if (DependsOn.Count > 0)
        {
            displayName = $"{displayName} dependsOn: {string.Join(", ", DependsOn.Select(t => t.Id))}";
        }

        return displayName;
    }
}
