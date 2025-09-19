using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation;

public class DocumentValidatorVisitor : SyntaxWalker<DocumentValidatorContext>
{
    protected DocumentValidatorVisitor(SyntaxVisitorOptions options = default)
        : base(Continue, options)
    {
    }

    protected override DocumentValidatorContext OnAfterEnter(
        ISyntaxNode node,
        ISyntaxNode? parent,
        DocumentValidatorContext context,
        ISyntaxVisitorAction action)
    {
        if (action.IsContinue())
        {
            context.Path.Push(node);
        }
        return context;
    }

    protected override DocumentValidatorContext OnBeforeLeave(
        ISyntaxNode node,
        ISyntaxNode? parent,
        DocumentValidatorContext context)
    {
        if (node.Kind == SyntaxKind.OperationDefinition)
        {
            context.Fragments.Reset();
        }
        context.Path.Pop();
        return context;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        for (var i = 0; i < node.Definitions.Count; i++)
        {
            if (node.Definitions[i].Kind != SyntaxKind.FragmentDefinition
                && Visit(node.Definitions[i], node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        if (base.VisitChildren(node, context).IsBreak())
        {
            return Break;
        }

        if (context.Fragments.TryEnter(node, out var fragment))
        {
            var result = Visit(fragment, node, context);
            context.Fragments.Leave(fragment);

            if (result.IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }
}
