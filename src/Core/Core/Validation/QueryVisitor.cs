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
        private readonly Dictionary<string, FragmentDefinitionNode> _fragments =
            new Dictionary<string, FragmentDefinitionNode>();
        private readonly HashSet<FragmentDefinitionNode> _visitedFragments =
            new HashSet<FragmentDefinitionNode>();
        private readonly HashSet<FragmentDefinitionNode> _touchedFragments =
            new HashSet<FragmentDefinitionNode>();

        protected QueryVisitor(ISchema schema)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        protected ISchema Schema { get; }

        public void VisitDocument(DocumentNode document)
        {
            ImmutableStack<ISyntaxNode> path =
                ImmutableStack<ISyntaxNode>.Empty.Push(document);

            // create fragment definition set.
            foreach (FragmentDefinitionNode fragment in document.Definitions
                .OfType<FragmentDefinitionNode>())
            {
                if (!_fragments.ContainsKey(fragment.Name.Value))
                {
                    _fragments[fragment.Name.Value] = fragment;
                }
            }

            VisitDocument(document, path);
        }

        protected virtual void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            VisitOperationDefinitions(
                document.Definitions.OfType<OperationDefinitionNode>(),
                path);

            VisitFragmentDefinitions(
                _fragments.Values,
                path);
        }

        protected virtual void VisitOperationDefinitions(
            IEnumerable<OperationDefinitionNode> oprationDefinitions,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (OperationDefinitionNode operation in oprationDefinitions)
            {
                VisitOperationDefinition(operation, path);
            }
        }

        protected virtual void VisitFragmentDefinitions(
            IEnumerable<FragmentDefinitionNode> fragmentDefinitions,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (FragmentDefinitionNode fragment in
                fragmentDefinitions.Where(
                    t => !_touchedFragments.Contains(t)))
            {
                VisitFragmentDefinition(fragment, path);
            }
        }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            IType operationType = Schema.GetOperationType(operation.Operation);
            if (operationType != null)
            {
                ImmutableStack<ISyntaxNode> newPath = path.Push(operation);

                VisitSelectionSet(
                    operation.SelectionSet,
                    operationType,
                    newPath);

                VisitDirectives(operation.Directives, newPath);
            }
            _visitedFragments.Clear();
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
                    else if (selection is FragmentSpreadNode fragmentSpread)
                    {
                        VisitFragmentSpread(fragmentSpread, type, newpath);
                    }
                    else if (selection is InlineFragmentNode inlineFragment)
                    {
                        VisitInlineFragmentInternal(
                            inlineFragment, type, newpath);
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
                && complexType.Fields.ContainsField(field.Name.Value)
                && field.SelectionSet != null)
            {
                VisitSelectionSet(field.SelectionSet,
                    complexType.Fields[field.Name.Value].Type.NamedType(),
                    newpath);
            }

            VisitDirectives(field.Directives, newpath);
        }

        protected virtual void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> newpath = path.Push(fragmentSpread);

            if (path.Last() is DocumentNode d)
            {
                string fragmentName = fragmentSpread.Name.Value;
                if (_fragments.TryGetValue(fragmentName,
                    out FragmentDefinitionNode fragment))
                {
                    VisitFragmentDefinition(fragment, newpath);
                }
            }

            VisitDirectives(fragmentSpread.Directives, newpath);
        }

        private void VisitInlineFragmentInternal(
            InlineFragmentNode inlineFragment,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (inlineFragment.TypeCondition?.Name?.Value == null)
            {
                VisitInlineFragment(inlineFragment, type, type, path);
            }
            else if (Schema.TryGetType(
                  inlineFragment.TypeCondition.Name.Value,
                  out INamedOutputType typeCondition))
            {
                VisitInlineFragment(inlineFragment, type, typeCondition, path);
            }
            else
            {
                VisitInlineFragment(inlineFragment, type, null, path);
            }
        }

        protected virtual void VisitInlineFragment(
            InlineFragmentNode inlineFragment,
            IType parentType,
            IType typeCondition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (typeCondition != null)
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
            if (MarkFragmentVisited(fragmentDefinition)
                && fragmentDefinition.TypeCondition?.Name?.Value != null
                && Schema.TryGetType(
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

        protected void ClearVisitedFragments()
        {
            _visitedFragments.Clear();
        }

        protected bool TryGetFragment(
            string fragmentName,
            out FragmentDefinitionNode fragment)
        {
            return _fragments.TryGetValue(fragmentName, out fragment);
        }

        protected bool ContainsFragment(string fragmentName)
        {
            return _fragments.ContainsKey(fragmentName);
        }

        protected bool IsFragmentVisited(
            FragmentDefinitionNode fragmentDefinition)
        {
            if (fragmentDefinition == null)
            {
                throw new ArgumentNullException(nameof(fragmentDefinition));
            }

            return _visitedFragments.Contains(fragmentDefinition);
        }

        protected bool MarkFragmentVisited(
            FragmentDefinitionNode fragmentDefinition)
        {
            if (fragmentDefinition == null)
            {
                throw new ArgumentNullException(nameof(fragmentDefinition));
            }

            _touchedFragments.Add(fragmentDefinition);
            return _visitedFragments.Add(fragmentDefinition);
        }
    }
}
