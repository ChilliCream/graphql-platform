using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class InlineFragmentPlanNode : SelectionPlanNode
{
    public InlineFragmentPlanNode(
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode> selectionNodes)
        : base(declaringType, selectionNodes)
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
