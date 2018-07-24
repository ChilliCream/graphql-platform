using System;
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
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        protected ISchema Schema { get; }

        public virtual void VisitDocument(DocumentNode document)
        {
            ImmutableStack<ISyntaxNode> path =
                ImmutableStack<ISyntaxNode>.Empty.Push(document);

            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation, path);
            }

            foreach (FragmentDefinitionNode fragment in document.Definitions
                .OfType<FragmentDefinitionNode>())
            {
                VisitFragmentDefinition(fragment, path);
            }
        }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            IType operationType = Schema.GetOperationType(operation.Operation);
            ImmutableStack<ISyntaxNode> newPath = path.Push(operation);
            VisitSelectionSet(operation.SelectionSet, operationType, newPath);
            VisitDirectives(operation.Directives, newPath);
        }

        protected virtual void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (selectionSet.Selections.Count > 0)
            {
                ImmutableStack<ISyntaxNode> newpath = path.Push(selectionSet);
                foreach (ISelectionNode selection in selectionSet.Selections)
                {

                    if (selection is FieldNode field)
                    {
                        VisitField(field, type, newpath);
                    }

                    if (selection is FragmentSpreadNode fragmentSpread)
                    {
                        VisitFragmentSpread(fragmentSpread, type, newpath);
                    }

                    if (selection is InlineFragmentNode inlineFragment)
                    {
                        VisitInlineFragment(inlineFragment, type, newpath);
                    }
                }
            }
        }

        protected virtual void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> newpath = path.Push(field);

            if (type is IComplexOutputType complexType
                && complexType.Fields.ContainsField(field.Name.Value))
            {
                if (field.SelectionSet != null)
                {
                    VisitSelectionSet(field.SelectionSet,
                        complexType.Fields[field.Name.Value].Type,
                        newpath);
                }
            }

            VisitDirectives(field.Directives, newpath);
        }

        protected virtual void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            VisitDirectives(fragmentSpread.Directives,
                path.Push(fragmentSpread));
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
                ImmutableStack<ISyntaxNode> newpath = path.Push(inlineFragment);

                VisitSelectionSet(
                    inlineFragment.SelectionSet,
                    typeCondition,
                    newpath);

                VisitDirectives(
                    inlineFragment.Directives,
                    newpath);
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
                ImmutableStack<ISyntaxNode> newpath = path
                    .Push(fragmentDefinition);

                VisitSelectionSet(
                    fragmentDefinition.SelectionSet,
                    typeCondition,
                    newpath);

                VisitDirectives(
                    fragmentDefinition.Directives,
                    newpath);
            }
        }

        protected virtual void VisitDirectives(
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
