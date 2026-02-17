using HotChocolate.Types;

namespace HotChocolate.Execution;

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
    /// Gets the declaring operation.
    /// </summary>
    IOperation DeclaringOperation { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Gets a value indicating whether this selection set contains any selections
    /// that may be deferred based on <c>@defer</c> directives.
    /// </summary>
    /// <value>
    /// <c>true</c> if one or more selections in this set can be deferred;
    /// otherwise, <c>false</c>.
    /// </value>
    bool HasIncrementalParts { get; }

    /// <summary>
    /// Gets the type that declares this selection set.
    /// </summary>
    IObjectTypeDefinition Type { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    IEnumerable<ISelection> GetSelections();
}
