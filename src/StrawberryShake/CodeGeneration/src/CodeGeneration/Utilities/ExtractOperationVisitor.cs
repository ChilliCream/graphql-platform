using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace StrawberryShake.CodeGeneration.Utilities;

internal class ExtractOperationVisitor : SyntaxWalker<ExtractOperationContext>
{
    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        ExtractOperationContext context)
    {
        context.ExportedFragments.Add(node);
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        ExtractOperationContext context)
    {
        if (base.VisitChildren(node, context).IsBreak())
        {
            return Break;
        }

        if (context.AllFragments.TryGetValue(
                node.Name.Value,
                out var fragment) &&
            context.VisitedFragments.Add(fragment.Name.Value))
        {
            if (Visit(fragment, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }
}
