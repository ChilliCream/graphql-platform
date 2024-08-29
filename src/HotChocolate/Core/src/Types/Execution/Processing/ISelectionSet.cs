#nullable enable

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A selection set is primarily composed of field selections.
/// When needed a selection set can preserve fragments so that the execution engine
/// can branch the processing of these fragments.
/// </summary>
public interface ISelectionSet
{
    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    IReadOnlyList<ISelection> Selections { get; }

    /// <summary>
    /// Gets the deferred fragments if any were preserved for execution.
    /// </summary>
    IReadOnlyList<IFragment> Fragments { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    IOperation DeclaringOperation { get; }
}
