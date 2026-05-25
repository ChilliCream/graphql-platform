using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The selection set index keeps track of the logical selection sets
/// that are used in the query plan.
/// </summary>
public interface ISelectionSetIndex
{
    /// <summary>
    /// Gets the ident
    /// </summary>
    /// <param name="selectionSet">The selection set node to resolve the identifier for.</param>
    /// <returns>The unique identifier of the selection set.</returns>
    uint GetId(SelectionSetNode selectionSet);

    bool TryGetOriginalIdFromCloned(uint clonedId, out uint originalId);

    bool IsRegistered(SelectionSetNode selectionSet);

    SelectionSetIndexBuilder ToBuilder();
}
