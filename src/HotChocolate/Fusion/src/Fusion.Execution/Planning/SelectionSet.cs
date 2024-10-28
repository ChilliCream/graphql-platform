using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSet : IOperationNode
{
    public SelectionSet(ImmutableArray<ISelection> selections)
    {
        Selections = selections;
    }

    public ImmutableArray<ISelection> Selections { get; }

    public SelectionSetNode ToSyntaxNode()
    {
        var selections = new ISelectionNode[Selections.Length];

        foreach (var selection in Selections)
        {
            selections[selections.Length] = selection.ToSyntaxNode();
        }

        return new SelectionSetNode(selections);
    }

    ISyntaxNode IOperationNode.ToSyntaxNode()
        => ToSyntaxNode();
}
