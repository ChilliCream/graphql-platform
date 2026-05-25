using HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

public interface ISelectionSetMergeObserver
{
    void OnMerge(FieldNode field1, FieldNode field2);

    void OnMerge(IEnumerable<FieldNode> fields);

    void OnMerge(InlineFragmentNode inlineFragment1, InlineFragmentNode inlineFragment2);

    void OnMerge(SelectionSetNode selectionSet1, SelectionSetNode selectionSet2);

    void OnMerge(IEnumerable<SelectionSetNode> selectionSets);
}
