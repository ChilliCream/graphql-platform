using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    public class RemoteQueryBuilder
    {
        private readonly List<FieldNode> _additionalFields =
            new List<FieldNode>();
        private readonly List<VariableDefinitionNode> _variables =
            new List<VariableDefinitionNode>();
        private readonly List<FragmentDefinitionNode> _fragments =
            new List<FragmentDefinitionNode>();
        private OperationType _operation = OperationType.Query;
        private IReadOnlyCollection<SelectionPathComponent> _path;
        private FieldNode _requestField;

        public RemoteQueryBuilder SetOperation(
            OperationType operation)
        {
            _operation = operation;
            return this;
        }

        public RemoteQueryBuilder SetSelectionPath(
            IReadOnlyCollection<SelectionPathComponent> selectionPath)
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

        public RemoteQueryBuilder AddVariable(NameString name, ITypeNode type)
        {
            return AddVariable(name, type, null);
        }

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
                defaultValue
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
            if (_additionalFields.Count > 0 && requestField.SelectionSet != null)
            {
                var selections = new List<ISelectionNode>(
                    requestField.SelectionSet.Selections);
                selections.AddRange(_additionalFields);
                requestField = requestField.WithSelectionSet(
                    requestField.SelectionSet.WithSelections(selections));
            }

            return CreateDelegationQuery(
                _operation, _path,
                requestField, _variables);
        }

        private DocumentNode CreateDelegationQuery(
            OperationType operation,
            IReadOnlyCollection<SelectionPathComponent> path,
            FieldNode requestedField,
            List<VariableDefinitionNode> variables)
        {
            var stack = new Stack<SelectionPathComponent>(path);

            if (!path.Any())
            {
                stack.Push(new SelectionPathComponent(
                    requestedField.Name,
                    Array.Empty<ArgumentNode>()));
            }

            FieldNode current = CreateRequestedField(stack, requestedField);

            while (path.Any())
            {
                current = CreateSelection(current, stack.Pop());
            }

            var definitions = new List<IDefinitionNode>();
            definitions.Add(CreateOperation(
                operation,
                new List<FieldNode> { current },
                _variables));
            definitions.AddRange(_fragments);

            return new DocumentNode(null, definitions);
        }

        private FieldNode CreateRequestedField(
            Stack<SelectionPathComponent> path,
            FieldNode requestedField)
        {
            SelectionPathComponent component = path.Pop();

            string responseName = requestedField.Alias == null
                ? requestedField.Name.Value
                : requestedField.Alias.Value;

            string alias = component.Name.Value.EqualsOrdinal(responseName)
                ? null
                : responseName;

            return CreateSelection(
                requestedField.SelectionSet,
                component,
                alias);
        }

        private FieldNode CreateSelection(
            FieldNode previous,
            SelectionPathComponent next)
        {
            var selectionSet = new SelectionSetNode(
                null,
                new List<ISelectionNode> { previous });

            return CreateSelection(selectionSet, next, null);
        }

        private FieldNode CreateSelection(
            SelectionSetNode selectionSet,
            SelectionPathComponent next,
            string alias)
        {
            var aliasNode = string.IsNullOrEmpty(alias)
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

        private OperationDefinitionNode CreateOperation(
            OperationType operation,
            List<FieldNode> fields,
            List<VariableDefinitionNode> variables)
        {
            return new OperationDefinitionNode(
                null,
                new NameNode("fetch"),
                operation,
                variables,
                new List<DirectiveNode>(),
                new SelectionSetNode(null, fields));
        }

        private IEnumerable<ArgumentNode> RewriteVariableNames(
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

        private Stack<SelectionPathComponent> GetSelectionPath(
            IDirectiveContext directiveContext)
        {
            var directive = directiveContext.Directive
                .ToObject<DelegateDirective>();

            if (string.IsNullOrEmpty(directive.Path))
            {
                return new Stack<SelectionPathComponent>();
            }

            return SelectionPathParser.Parse(new Source(directive.Path));
        }

        public static RemoteQueryBuilder New() => new RemoteQueryBuilder();
    }
}
