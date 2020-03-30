using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation
{
    public class DocumentValidatorVisitor
        : SyntaxWalker<IDocumentValidatorContext>
    {
        protected DocumentValidatorVisitor()
            : base(Continue)
        {
        }

        protected override IDocumentValidatorContext OnAfterEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            context.Path.Push(node);
            return context;
        }

        protected override IDocumentValidatorContext OnBeforeLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidatorContext context)
        {
            context.Path.Pop();
            return context;
        }

        protected override IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Document:
                    return ((DocumentNode)node).Definitions.Where(t =>
                        t.Kind != NodeKind.FragmentDefinition);

                case NodeKind.FragmentSpread:
                    return GetFragmentSpreadChildren((FragmentSpreadNode)node, context);

                default:
                    return node.GetNodes();
            }
        }

        private static IEnumerable<ISyntaxNode> GetFragmentSpreadChildren(
            FragmentSpreadNode fragmentSpread,
            IDocumentValidatorContext context)
        {
            foreach (ISyntaxNode child in fragmentSpread.GetNodes())
            {
                yield return child;
            }

            if (context.Fragments.TryGetValue(
                fragmentSpread.Name.Value,
                out FragmentDefinitionNode? fragment))
            {
                yield return fragment;
            }
        }
    }
}
