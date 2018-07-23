using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal class QueryVisitor
    {
        protected QueryVisitor(ISchema schema)
        {
            Schema = schema
                ?? throw new System.ArgumentNullException(nameof(schema));
        }

        protected ISchema Schema { get; }

        public virtual void VisitDocument(DocumentNode document)
        {
            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation,
                    ImmutableStack<ISyntaxNode>.Empty.Push(document));
            }

            foreach (FragmentDefinitionNode fragment in document.Definitions
                .OfType<FragmentDefinitionNode>())
            {
                VisitFragmentDefinition(fragment,
                    ImmutableStack<ISyntaxNode>.Empty.Push(document));
            }
        }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            IType operationType = Schema.GetOperationType(operation.Operation);
            VisitSelectionSet(operation.SelectionSet, operationType,
                path.Push(operation));
        }

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
                    VisitFragmentSpread(fragmentSpread, type, path);
                }

                if (selection is InlineFragmentNode inlineFragment)
                {
                    VisitInlineFragment(inlineFragment, type, path);
                }
            }
        }

        protected virtual void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> current = path.Push(field);

            if (type is IComplexOutputType complexType
                && complexType.Fields.ContainsField(field.Name.Value))
            {
                if (field.SelectionSet != null)
                {
                    VisitSelectionSet(field.SelectionSet,
                        complexType.Fields[field.Name.Value].Type,
                        path);
                }
            }

            VisitDirectives(field.Directives, path.Push(field));
        }

        protected virtual void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {

        }

        protected virtual void VisitInlineFragment(
            InlineFragmentNode inlineFragment,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (inlineFragment.TypeCondition?.Name?.Value != null
                && Schema.TryGetType<INamedOutputType>(
                    inlineFragment.TypeCondition.Name.Value,
                    out INamedOutputType typeCondition))
            {
                VisitSelectionSet(
                    inlineFragment.SelectionSet,
                    typeCondition,
                    path.Push(inlineFragment));

                VisitDirectives(
                    inlineFragment.Directives,
                    path.Push(inlineFragment));
            }
        }

        protected virtual void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (fragmentDefinition.TypeCondition?.Name?.Value != null
                && Schema.TryGetType<INamedOutputType>(
                    fragmentDefinition.TypeCondition.Name.Value,
                    out INamedOutputType typeCondition))
            {
                VisitSelectionSet(
                    fragmentDefinition.SelectionSet,
                    typeCondition,
                    path.Push(fragmentDefinition));

                VisitDirectives(fragmentDefinition.Directives,
                    path.Push(fragmentDefinition));
            }
        }

        private void VisitDirectives(
            IReadOnlyCollection<DirectiveNode> directives,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (DirectiveNode directive in directives)
            {
                VisitDirective(directive, path);
            }
        }

        protected virtual void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {

        }
    }
}
