using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class InlineFragmentPlanNode : SelectionPlanNode
{
    public InlineFragmentPlanNode(
        ICompositeNamedType declaringType,
        InlineFragmentNode inlineFragment)
        : this(declaringType, inlineFragment.Directives, inlineFragment.SelectionSet.Selections)
    {
    }

    public InlineFragmentPlanNode(
        ICompositeNamedType declaringType,
        IReadOnlyList<DirectiveNode> directiveNodes,
        IReadOnlyList<ISelectionNode> selectionNodes)
        : base(declaringType, directiveNodes, selectionNodes)
    {
    }

    public InlineFragmentNode ToSyntaxNode()
    {
        return new InlineFragmentNode(
            null,
            new NamedTypeNode(DeclaringType.Name),
            Directives.ToSyntaxNode(),
            Selections.ToSyntaxNode());
    }
}
