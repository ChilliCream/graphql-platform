using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation
{
    public class DocumentValidatorVisitor
        : SyntaxWalker<IDocumentValidatorContext>
    {
        protected DocumentValidatorVisitor(SyntaxVisitorOptions options = default)
            : base(Continue, options)
        {
        }

        protected override IDocumentValidatorContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IDocumentValidatorContext context,
            ISyntaxVisitorAction action)
        {
            if (action.IsContinue())
            {
                context.Path.Push(node);
            }
            return context;
        }

        protected override IDocumentValidatorContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IDocumentValidatorContext context)
        {
            if (node.Kind == SyntaxKind.OperationDefinition)
            {
                context.VisitedFragments.Clear();
            }
            context.Path.Pop();
            return context;
        }

        protected override ISyntaxVisitorAction VisitChildren(
            DocumentNode node,
            IDocumentValidatorContext context)
        {
            for (int i = 0; i < node.Definitions.Count; i++)
            {
                if (node.Definitions[i].Kind != SyntaxKind.FragmentDefinition &&
                    Visit(node.Definitions[i], node, context).IsBreak())
                {
                    return Break;
                }
            }

            return DefaultAction;
        }

        protected override ISyntaxVisitorAction VisitChildren(
            FragmentSpreadNode node,
            IDocumentValidatorContext context)
        {
            if (base.VisitChildren(node, context).IsBreak())
            {
                return Break;
            }

            if (context.Fragments.TryGetValue(
                node.Name.Value,
                out FragmentDefinitionNode? fragment) &&
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
}
