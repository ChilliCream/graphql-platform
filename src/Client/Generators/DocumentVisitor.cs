using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class DocumentVisitor
        : SyntaxNodeVisitor
    {
        private readonly Stack<IType> _types =
            new Stack<IType>();
        private readonly HashSet<string> _typeNames = new HashSet<string>();
        private readonly Stack<SelectionSetNode> _selectionSets =
            new Stack<SelectionSetNode>();
        private readonly TypeRegistry _typeRegistry = new TypeRegistry();

        private readonly ISchema _schema;
        private readonly Dictionary<string, string> _scalarTypes =
            new Dictionary<string, string>();

        private SelectionSetNode _currentSelectionSet;

        public override VisitorAction Enter(
            OperationDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
           FieldNode node,
           ISyntaxNode parent,
           IReadOnlyList<object> path,
           IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_types.Peek().NamedType() is IComplexOutputType complexType
                && complexType.Fields.TryGetField(
                    node.Name.Value, out IOutputField field))
            {
                _types.Push(field.Type);
                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }

        public override VisitorAction Leave(
            FieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
          InlineFragmentNode node,
          ISyntaxNode parent,
          IReadOnlyList<object> path,
          IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedType type))
            {
                _types.Push(type);
                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }

        public override VisitorAction Leave(
            InlineFragmentNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
          FragmentDefinitionNode node,
          ISyntaxNode parent,
          IReadOnlyList<object> path,
          IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out INamedType type))
            {
                _types.Push(type);
                return VisitorAction.Continue;
            }

            return VisitorAction.Break;
        }

        public override VisitorAction Leave(
            FragmentDefinitionNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            _types.Pop();
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

        private void AddTypeName(IOutputField field, FieldNode selection)
        {
            string t field.Type.NamedType().



            // while(
        }

        
    }
}
