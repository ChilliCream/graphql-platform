using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal static class SelectionSetCloner
{
    // Clone top-down so every nested selection set can be registered against the original node.
    // A bottom-up syntax rewrite replaces children before the parent is visited and loses that mapping.
    public static SelectionSetNode Clone(
        SelectionSetNode original,
        SelectionSetIndexBuilder indexBuilder)
    {
        var clonedSelections = new ISelectionNode[original.Selections.Count];

        for (var i = 0; i < original.Selections.Count; i++)
        {
            clonedSelections[i] = CloneSelection(original.Selections[i], indexBuilder);
        }

        var cloned = new SelectionSetNode(clonedSelections);
        indexBuilder.RegisterCloned(original, cloned);
        return cloned;
    }

    private static ISelectionNode CloneSelection(
        ISelectionNode selection,
        SelectionSetIndexBuilder indexBuilder)
    {
        return selection switch
        {
            FieldNode field when field.SelectionSet is not null
                => field.WithSelectionSet(Clone(field.SelectionSet, indexBuilder)),
            InlineFragmentNode fragment
                => fragment.WithSelectionSet(Clone(fragment.SelectionSet, indexBuilder)),
            _ => selection
        };
    }
}
