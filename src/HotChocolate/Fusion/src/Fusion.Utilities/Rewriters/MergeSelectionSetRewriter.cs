using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class MergeSelectionSetRewriter(
    ISchemaDefinition schema,
    bool ignoreMissingTypeSystemMembers = false)
{
    private readonly InlineFragmentOperationRewriter _rewriter =
        new(schema, ignoreMissingTypeSystemMembers: ignoreMissingTypeSystemMembers);

    public SelectionSetNode Merge(
        SelectionSetNode selectionSet1,
        SelectionSetNode selectionSet2,
        ITypeDefinition type,
        ISelectionSetMergeObserver? mergeObserver)
    {
        mergeObserver ??= NoopSelectionSetMergeObserver.Instance;

        var selectionCount = selectionSet1.Selections.Count + selectionSet2.Selections.Count;
        var selections = new ISelectionNode[selectionCount];

        CopyTo(selectionSet1.Selections, selections, 0);
        CopyTo(selectionSet2.Selections, selections, selectionSet1.Selections.Count);

        var hasIncrementalParts = false;
        var context = new InlineFragmentOperationRewriter.Context(type, [], ref hasIncrementalParts, mergeObserver);
        var merged = new SelectionSetNode(null, selections);

        mergeObserver.OnMerge(selectionSet1, selectionSet2);
        mergeObserver.OnMerge(selectionSet1, merged);

        _rewriter.CollectSelections(merged, context);
        _rewriter.RewriteSelections(context);

        return new SelectionSetNode(null, context.Selections.ToImmutable());
    }

    public SelectionSetNode Merge(
        IReadOnlyList<SelectionSetNode> selectionSets,
        ITypeDefinition type,
        ISelectionSetMergeObserver? mergeObserver = null)
    {
        mergeObserver ??= NoopSelectionSetMergeObserver.Instance;

        var hasIncrementalParts = false;
        var context = new InlineFragmentOperationRewriter.Context(type, [], ref hasIncrementalParts, mergeObserver);
        var merged = new SelectionSetNode(null, [.. selectionSets.SelectMany(t => t.Selections)]);

        mergeObserver.OnMerge(selectionSets);
        mergeObserver.OnMerge(selectionSets[0], merged);

        _rewriter.CollectSelections(merged, context);
        _rewriter.RewriteSelections(context);

        return new SelectionSetNode(null, context.Selections.ToImmutable());
    }

    private static void CopyTo(IReadOnlyList<ISelectionNode> source, ISelectionNode[] target, int offset)
    {
        for (var i = 0; i < source.Count; i++)
        {
            target[i + offset] = source[i];
        }
    }
}
