using HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

internal sealed class NoopSelectionSetMergeObserver : ISelectionSetMergeObserver
{
    public void OnMerge(FieldNode field1, FieldNode field2)
    {
    }

    public void OnMerge(IEnumerable<FieldNode> fields)
    {
    }

    public void OnMerge(InlineFragmentNode inlineFragment1, InlineFragmentNode inlineFragment2)
    {
    }

    public void OnMerge(SelectionSetNode selectionSet1, SelectionSetNode selectionSet2)
    {
    }

    public void OnMerge(IEnumerable<SelectionSetNode> selectionSets)
    {
    }

    public static NoopSelectionSetMergeObserver Instance { get; } = new();
}
