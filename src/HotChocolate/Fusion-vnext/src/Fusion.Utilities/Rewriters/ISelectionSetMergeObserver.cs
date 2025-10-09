using HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

public interface ISelectionSetMergeObserver
{
    void OnMerge(SelectionSetNode newSelectionSetNode, params IEnumerable<SelectionSetNode> oldSelectionSetNodes);
}
