using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents a execution step within the execution plan while being in the planing phase.
/// After the planing phase execution steps are compiled into execution nodes.
/// </summary>
internal abstract class ExecutionStep
{
    protected ExecutionStep(ObjectTypeInfo selectionSetTypeInfo, ISelection? parentSelection)
    {
        SelectionSetTypeInfo = selectionSetTypeInfo ??
            throw new ArgumentNullException(nameof(selectionSetTypeInfo));
        ParentSelection = parentSelection;
    }

    /// <summary>
    /// Gets the declaring type of the root selection set of this execution step.
    /// </summary>
    public ObjectTypeInfo SelectionSetTypeInfo { get; }

    /// <summary>
    /// Gets the parent selection.
    /// </summary>
    public ISelection? ParentSelection { get; }

    /// <summary>
    /// Gets the execution steps this execution step is depending on.
    /// </summary>
    public HashSet<ExecutionStep> DependsOn { get; } = new();
}
