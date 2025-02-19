using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public class MergeSelectionSetRewriter(ISchemaDefinition schema)
{
    private readonly InlineFragmentOperationRewriter _rewriter = new(schema);

    public SelectionSetNode Merge(
        SelectionSetNode selectionSet1,
        SelectionSetNode selectionSet2,
        ITypeDefinition type)
    {
        var selectionCount = selectionSet1.Selections.Count + selectionSet2.Selections.Count;
        var selections = new ISelectionNode[selectionCount];

        CopyTo(selectionSet1.Selections, selections, 0);
        CopyTo(selectionSet2.Selections, selections, selectionSet1.Selections.Count);

        var context = new InlineFragmentOperationRewriter.Context(type, []);
        var merged = new SelectionSetNode(null, selections);

        _rewriter.CollectSelections(merged, context);
        _rewriter.RewriteSelections(context);

        return new SelectionSetNode(null, context.Selections.ToImmutable());
    }

    public SelectionSetNode Merge(
        IReadOnlyList<SelectionSetNode> selectionSets,
        ITypeDefinition type)
    {
        var context = new InlineFragmentOperationRewriter.Context(type, []);
        var merged = new SelectionSetNode(null, [.. selectionSets.SelectMany(t => t.Selections)]);

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
