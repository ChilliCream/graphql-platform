using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public class MergeSelectionSetRewriter(ISchemaDefinition schema)
{
    private readonly InlineFragmentOperationRewriter _rewriter = new(schema);

    public SelectionSetNode RewriteSelectionSets(
        IReadOnlyList<SelectionSetNode> selectionSets,
        IReadOnlyNamedTypeDefinition type)
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
