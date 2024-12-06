using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class InlineFragmentPlanNode : SelectionPlanNode
{
    public InlineFragmentPlanNode(
        InlineFragmentNode inlineFragmentNode,
        ICompositeNamedType declaringType)
        : base(declaringType, inlineFragmentNode.SelectionSet.Selections, inlineFragmentNode.Directives)
    {
    }

    public InlineFragmentNode ToSyntaxNode()
    {
        var directives = new List<DirectiveNode>(Directives.ToSyntaxNode());
        ExtendDirectivesWithConditions(directives);

        return new InlineFragmentNode(
            null,
            new NamedTypeNode(DeclaringType.Name),
            directives,
            Selections.ToSyntaxNode());
    }
}
