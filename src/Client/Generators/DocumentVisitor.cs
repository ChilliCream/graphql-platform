using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators
{
    public class DocumentVisitor
        : SyntaxNodeVisitor
    {
        private readonly Stack<SelectionSetNode> _selectionSets =
            new Stack<SelectionSetNode>();


        public override VisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            SelectionSetNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _selectionSets.Push(node);
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            SelectionSetNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _selectionSets.Pop();
            return VisitorAction.Continue;
        }
    }
}
