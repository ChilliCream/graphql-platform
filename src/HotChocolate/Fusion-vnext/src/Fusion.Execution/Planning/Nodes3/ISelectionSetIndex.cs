using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public interface ISelectionSetIndex
{
    uint GetId(SelectionSetNode selectionSet);

    bool TryGetId(SelectionSetNode selectionSet, out uint id);

    bool IsRegistered(SelectionSetNode selectionSet);

    SelectionSetIndexBuilder ToBuilder();
}
