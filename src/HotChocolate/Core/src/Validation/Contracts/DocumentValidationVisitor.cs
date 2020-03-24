using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation
{
    public class DocumentValidationVisitor
        : SyntaxWalker<IDocumentValidationContext>
    {
        private static readonly ISyntaxNode[] _fragmentNodes = new ISyntaxNode[1];

        protected override IDocumentValidationContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidationContext context)
        {
            context.Path.Push(node);
            return context;
        }

        protected override IDocumentValidationContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            IDocumentValidationContext context)
        {
            context.Path.Pop();
            return context;
        }

        protected override IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            IDocumentValidationContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Document:
                    return ((DocumentNode)node).Definitions.Where(t =>
                        t.Kind != NodeKind.FragmentDefinition);

                case NodeKind.FragmentSpread:
                    if(context.Fragments.TryGetValue(
                        ((FragmentSpreadNode)node).Name.Value,
                        out FragmentDefinitionNode? fragment))
                    {
                        _fragmentNodes[0] = fragment;
                        return _fragmentNodes;
                    }
                    return Enumerable.Empty<ISyntaxNode>();

                default:
                    return node.GetNodes();
            }
        }
    }
}
