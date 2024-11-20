using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class InlineFragmentPlanNode : SelectionPlanNode
{
    public InlineFragmentPlanNode(
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode> selectionNodes)
        : base(declaringType, selectionNodes)
    {
    }
}
