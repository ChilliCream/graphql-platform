using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public interface ISelectionSetIndex
{
    int GetId(SelectionSetNode selectionSet);

    bool TryGetId(SelectionSetNode selectionSet, out int id);

    bool IsRegistered(SelectionSetNode selectionSet);

    SelectionSetIndexBuilder ToBuilder();
}
