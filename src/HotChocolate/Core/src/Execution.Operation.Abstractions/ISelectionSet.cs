using HotChocolate.Types;

namespace HotChocolate.Execution;

public interface ISelectionSet
{
    /// <summary>
    /// Gets an operation unique selection-set identifier of this selection.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets the declaring operation.
    /// </summary>
    IOperation DeclaringOperation { get; }

    /// <summary>
    /// Defines if this list needs post-processing for skip and include.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Gets the type that declares this selection set.
    /// </summary>
    IObjectTypeDefinition Type { get; }

    /// <summary>
    /// Gets the selections that shall be executed.
    /// </summary>
    IEnumerable<ISelection> GetSelections();
}
