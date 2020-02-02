using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteQueryBuilder
    {
        private static readonly CollectUsedVariableVisitor _usedVariables =
            new CollectUsedVariableVisitor();
        private readonly List<FieldNode> _additionalFields =
            new List<FieldNode>();
        private readonly List<VariableDefinitionNode> _variables =
            new List<VariableDefinitionNode>();
        private readonly List<FragmentDefinitionNode> _fragments =
            new List<FragmentDefinitionNode>();
        private NameNode _operationName = new NameNode("fetch");
        private OperationType _operation = OperationType.Query;
        private IImmutableStack<SelectionPathComponent> _path =
            ImmutableStack<SelectionPathComponent>.Empty;
        private FieldNode _requestField;

        public RemoteQueryBuilder SetOperation(
            NameNode name,
            OperationType operation)
        {
            if (name != null)
            {
                _operationName = name;
            }
            
            _operation = operation;
            return this;
        }

        public RemoteQueryBuilder SetSelectionPath(
            IImmutableStack<SelectionPathComponent> selectionPath)
        {
            if (selectionPath == null)
            {
                throw new ArgumentNullException(nameof(selectionPath));
            }
            _path = selectionPath;
            return this;
        }

        public RemoteQueryBuilder SetRequestField(FieldNode field)
        {
            _requestField = field
                ?? throw new ArgumentNullException(nameof(field));
            return this;
        }

        public RemoteQueryBuilder AddAdditionalField(
            FieldNode field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            _additionalFields.Add(field);
            return this;
        }

        public RemoteQueryBuilder AddVariable(
            NameString name, ITypeNode type) =>
            AddVariable(name, type, null);

        public RemoteQueryBuilder AddVariable(
            NameString name,
            ITypeNode type,
            IValueNode defaultValue)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            name.EnsureNotEmpty(nameof(name));

            AddVariable(new VariableDefinitionNode
            (
                null,
                new VariableNode(new NameNode(name)),
                type,
                defaultValue,
                Array.Empty<DirectiveNode>()
            ));

            return this;
        }

        public RemoteQueryBuilder AddVariable(
            VariableDefinitionNode variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            _variables.Add(variable);

            return this;
        }

        public RemoteQueryBuilder AddVariables(
            IEnumerable<VariableDefinitionNode> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            _variables.AddRange(variables);

            return this;
        }

        public RemoteQueryBuilder AddFragmentDefinitions(
            IEnumerable<FragmentDefinitionNode> fragments)
        {
            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            _fragments.AddRange(fragments);

            return this;
        }

        public DocumentNode Build()
        {
            if (_requestField == null || _path == null)
            {
                throw new InvalidOperationException();
            }

            FieldNode requestField = _requestField;
            if (_additionalFields.Count != 0
                && requestField.SelectionSet != null)
            {
                var selections = new List<ISelectionNode>(
                    requestField.SelectionSet.Selections);
                selections.AddRange(_additionalFields);
                requestField = requestField.WithSelectionSet(
                    requestField.SelectionSet.WithSelections(selections));
            }

            return CreateDelegationQuery(_operation, _path, requestField);
        }

        private DocumentNode CreateDelegationQuery(
            OperationType operation,
            IImmutableStack<SelectionPathComponent> path,
            FieldNode requestedField)
        {
            if (!path.Any())
            {
                path = path.Push(new SelectionPathComponent(
                    requestedField.Name,
                    Array.Empty<ArgumentNode>()));
            }

            FieldNode current = CreateRequestedField(requestedField, ref path);

            while (path.Any())
            {
                path = path.Pop(out SelectionPathComponent component);
                current = CreateSelection(current, component);
            }

            var usedVariables = new HashSet<string>();
            _usedVariables.CollectUsedVariables(current, usedVariables);
            _usedVariables.CollectUsedVariables(_fragments, usedVariables);

            var definitions = new List<IDefinitionNode>();
            definitions.Add(CreateOperation(
                _operationName,
                operation,
                new List<FieldNode> { current },
                _variables.Where(t =>
                    usedVariables.Contains(t.Variable.Name.Value))
                    .ToList()));
            definitions.AddRange(_fragments);

            return new DocumentNode(null, definitions);
        }

        private static FieldNode CreateRequestedField(
            FieldNode requestedField,
            ref IImmutableStack<SelectionPathComponent> path)
        {
            path = path.Pop(out SelectionPathComponent component);

            string responseName = requestedField.Alias == null
                ? requestedField.Name.Value
                : requestedField.Alias.Value;

            NameNode alias = component.Name.Value.EqualsOrdinal(responseName)
                ? null
                : new NameNode(responseName);

            IReadOnlyList<ArgumentNode> arguments =
                component.Arguments.Count == 0
                ? requestedField.Arguments
                : RewriteVariableNames(component.Arguments).ToList();

            return new FieldNode
            (
                null,
                component.Name,
                alias,
                requestedField.Directives,
                arguments,
                requestedField.SelectionSet
            );
        }

        private static FieldNode CreateSelection(
            FieldNode previous,
            SelectionPathComponent next)
        {
            var selectionSet = new SelectionSetNode(
                null,
                new List<ISelectionNode> { previous });

            return CreateSelection(selectionSet, next, null);
        }

        private static FieldNode CreateSelection(
            SelectionSetNode selectionSet,
            SelectionPathComponent next,
            string alias)
        {
            NameNode aliasNode = string.IsNullOrEmpty(alias)
                ? null : new NameNode(alias);

            return new FieldNode
            (
                null,
                next.Name,
                aliasNode,
                Array.Empty<DirectiveNode>(),
                RewriteVariableNames(next.Arguments).ToList(),
                selectionSet
            );
        }

        private static OperationDefinitionNode CreateOperation(
            NameNode name,
            OperationType operation,
            IReadOnlyList<FieldNode> fields,
            IReadOnlyList<VariableDefinitionNode> variables)
        {
            return new OperationDefinitionNode(
                null,
                name,
                operation,
                variables,
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(null, fields));
        }

        private static IEnumerable<ArgumentNode> RewriteVariableNames(
            IEnumerable<ArgumentNode> arguments)
        {
            foreach (ArgumentNode argument in arguments)
            {
                if (argument.Value is ScopedVariableNode v)
                {
                    yield return argument.WithValue(v.ToVariableNode());
                }
                else
                {
                    yield return argument;
                }
            }
        }

        public static RemoteQueryBuilder New() => new RemoteQueryBuilder();
    }
}
