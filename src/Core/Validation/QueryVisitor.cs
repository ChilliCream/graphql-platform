using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal class QueryVisitor
    {
        private readonly ISchema _schema;

        public QueryVisitor(ISchema schema)
        {
            _schema = schema
                ?? throw new System.ArgumentNullException(nameof(schema));
        }

        protected virtual void VisitDocument(DocumentNode node)
        {
            foreach (OperationDefinitionNode operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation,
                    ImmutableStack<ISyntaxNode>.Empty);
            }
        }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            // IType operationType = _schema.GetOperationType(operation.Operation);
            VisitSelectionSet(operation.SelectionSet, null, path);
        }

        protected virtual void VisitVariableDefinition(VariableDefinitionNode node) { }

        protected virtual void VisitVariable(VariableNode node) { }

        protected virtual void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                if (selection is FieldNode field)
                {
                    VisitField(field, type, path);
                }

                if (selection is FragmentSpreadNode fragmentSpread)
                {
                    VisitFragmentSpread(fragmentSpread, type);
                }

                if (selection is InlineFragmentNode inlineFragment)
                {
                    VisitInlineFragment(inlineFragment, type);
                }
            }
        }

        protected virtual void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> current = path.Push(field);

            foreach (ArgumentNode argument in field.Arguments)
            {
                VisitArgument(argument, type, current);
            }

            if (type is IComplexOutputType complexType
                && complexType.Fields.ContainsField(field.Name.Value))
            {
                VisitSelectionSet(field.SelectionSet,
                    complexType.Fields[field.Name.Value].Type,
                    path);
            }
        }

        protected virtual void VisitArgument(
            ArgumentNode node,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {

        }

        protected virtual void VisitFragmentSpread(FragmentSpreadNode fragmentSpread, IType type) { }
        protected virtual void VisitInlineFragment(InlineFragmentNode node, IType type) { }
        protected virtual void VisitFragmentDefinition(FragmentDefinitionNode node) { }
    }
}
