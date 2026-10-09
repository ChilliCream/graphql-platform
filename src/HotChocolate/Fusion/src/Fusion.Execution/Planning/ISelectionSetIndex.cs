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

    /// <summary>
    /// Tries to resolve a selection set by its logical identifier.
    /// </summary>
    /// <param name="id">The logical selection set identifier.</param>
    /// <param name="selectionSet">
    /// The selection set associated with <paramref name="id"/>, if one is registered.
    /// </param>
    /// <returns>
    /// <c>true</c> if a selection set is registered for <paramref name="id"/>, otherwise <c>false</c>.
    /// </returns>
    bool TryGetSelectionSet(uint id, out SelectionSetNode selectionSet);

    /// <summary>
    /// Tries to resolve the original selection set identifier for a cloned selection set.
    /// </summary>
    /// <param name="clonedId">The logical identifier of the cloned selection set.</param>
    /// <param name="originalId">
    /// The logical identifier of the original selection set, if <paramref name="clonedId"/>
    /// belongs to a clone.
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="clonedId"/> maps to an original selection set,
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryGetOriginalIdFromCloned(uint clonedId, out uint originalId);

    /// <summary>
    /// Determines whether the specified selection set is registered in this index.
    /// </summary>
    /// <param name="selectionSet">The selection set to check.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="selectionSet"/> is registered, otherwise <c>false</c>.
    /// </returns>
    bool IsRegistered(SelectionSetNode selectionSet);

    /// <summary>
    /// Creates a mutable builder initialized from the current index state.
    /// </summary>
    /// <returns>A selection set index builder for adding cloned or merged selection sets.</returns>
    SelectionSetIndexBuilder ToBuilder();
}
