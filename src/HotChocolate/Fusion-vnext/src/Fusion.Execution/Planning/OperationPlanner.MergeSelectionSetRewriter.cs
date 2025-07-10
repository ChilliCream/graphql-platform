using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private static readonly HasEmptySelectionSetVisitor s_hasEmptySelectionSetVisitor = new();

    private sealed class HasEmptySelectionSetVisitor : SyntaxWalker<HasEmptySelectionSetVisitor.Context>
    {
        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            Context context)
        {
            if (node is SelectionSetNode { Selections.Count: 0 })
            {
                context.HasEmptySelectionSet = true;
                return Break;
            }

            return base.Enter(node, context);
        }

        public sealed class Context
        {
            public bool HasEmptySelectionSet { get; set; }
        }
    }
}
