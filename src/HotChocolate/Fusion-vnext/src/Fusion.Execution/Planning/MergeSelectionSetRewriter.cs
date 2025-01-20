using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class MergeSelectionSetRewriter(CompositeSchema schema)
{
    private readonly InlineFragmentOperationRewriter _rewriter = new(schema);

    public SelectionSetNode Merge(
        SelectionSetNode selectionSet1,
        SelectionSetNode selectionSet2,
        ICompositeNamedType type)
        => Merge([selectionSet1, selectionSet2], type);

    public SelectionSetNode Merge(
        IReadOnlyList<SelectionSetNode> selectionSets,
        ICompositeNamedType type)
    {
        var context = new InlineFragmentOperationRewriter.Context(
            type,
            new Dictionary<string, FragmentDefinitionNode>());

        var merged = new SelectionSetNode(
            null,
            selectionSets.SelectMany(t => t.Selections).ToList());

        _rewriter.CollectSelections(merged, context);
        _rewriter.RewriteSelections(context);

        return new SelectionSetNode(
            null,
            context.Selections.ToImmutable());
    }
}
