using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class InlineFragment : ISelection
{
    public InlineFragment(
        ICompositeNamedType type,
        DirectiveCollection directives,
        SelectionSet selectionSet)
    {
        Type = type;
        Directives = directives;
        SelectionSet = selectionSet;
    }

    public ICompositeNamedType Type { get; }

    public DirectiveCollection Directives { get; }


    public SelectionSet SelectionSet { get; }

    public InlineFragmentNode ToSyntaxNode()
    {
        return new InlineFragmentNode(
            null,
            new NamedTypeNode(new NameNode(Type.Name)),
            Directives.ToSyntaxNodes(),
            SelectionSet.ToSyntaxNode());
    }

    ISelectionNode ISelection.ToSyntaxNode()
        => ToSyntaxNode();

    ISyntaxNode IOperationNode.ToSyntaxNode()
        => ToSyntaxNode();
}
