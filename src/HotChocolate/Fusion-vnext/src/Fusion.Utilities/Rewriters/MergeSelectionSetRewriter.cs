using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class MergeSelectionSetRewriter(ISchemaDefinition schema)
{
    private readonly DocumentRewriter _documentRewriter = new(schema);

    public SelectionSetNode Merge(
        SelectionSetNode selectionSet1,
        SelectionSetNode selectionSet2,
        ITypeDefinition type,
        ISelectionSetMergeObserver? mergeObserver)
        => Merge([selectionSet1, selectionSet2], type, mergeObserver);

    public SelectionSetNode Merge(
        IReadOnlyList<SelectionSetNode> selectionSets,
        ITypeDefinition type,
        ISelectionSetMergeObserver? mergeObserver = null)
    {
        var selectionSetNode = new SelectionSetNode([
            .. selectionSets.SelectMany(t => t.Selections)
        ]);

        var newSelectionSetNode = _documentRewriter.RewriteSelectionSet(
            selectionSetNode,
            type,
            mergeObserver);

        return newSelectionSetNode;
    }
}
