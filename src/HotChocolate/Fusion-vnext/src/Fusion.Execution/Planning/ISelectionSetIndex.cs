using HotChocolate.Fusion.Planning.Nodes3;
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
    /// <param name="selectionSet"></param>
    /// <returns></returns>
    uint GetId(SelectionSetNode selectionSet);

    bool TryGetId(SelectionSetNode selectionSet, out uint id);

    bool IsRegistered(SelectionSetNode selectionSet);

    SelectionSetIndexBuilder ToBuilder();
}
