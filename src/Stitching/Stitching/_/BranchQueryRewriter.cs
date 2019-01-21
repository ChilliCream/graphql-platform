using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    public class BranchQueryRewriter

    {

    }

    public class RemoteQueryBuilder
    {
        private readonly List<FieldNode> _fields = new List<FieldNode>();
        private OperationType _operation = OperationType.Query;
        private Stack<SelectionPathComponent> _path;


        public RemoteQueryBuilder SetOperation(
            OperationType operation)
        {
            _operation = operation;
            return this;
        }

        public RemoteQueryBuilder SetSelectionPath(
            Stack<SelectionPathComponent> selectionPath)
        {
            if (selectionPath == null)
            {
                throw new ArgumentNullException(nameof(selectionPath));
            }
            _path = selectionPath;
            return this;
        }

        public RemoteQueryBuilder AddField(
            FieldNode field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            _fields.Add(field);
            return this;
        }

        public DocumentNode Build()
        {
            if (_path == null || _fields.Count == 0)
            {
                throw new InvalidOperationException();
            }
            return CreateDelegationQuery(_operation, _path, _fields);
        }

        private DocumentNode CreateDelegationQuery(
           OperationType operation,
           Stack<SelectionPathComponent> path,
           List<FieldNode> selections)
        {
            FieldNode current = null;
            var currentSelections = new SelectionSetNode(null, selections);

            if (path.Any())
            {
                string responseName = current.Alias == null
                    ? current.Name.Value
                    : current.Alias.Value;

                SelectionPathComponent component = path.Pop();

                string alias = component.Name.Value.EqualsOrdinal(responseName)
                    ? null
                    : responseName;

                current = CreateSelection(
                    currentSelections,
                    component,
                    responseName);

                while (path.Any())
                {
                    current = CreateSelection(current, path.Pop());
                }
            }

            return new DocumentNode(
                null,
                new List<IDefinitionNode>
                {
                    CreateOperation(operation, current)
                });
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
            FieldNode field
            )
        {
            var selectionSet = new SelectionSetNode(
                null,
                new List<ISelectionNode> { field });

            return new OperationDefinitionNode(
                null,
                null,
                operation,
                new List<VariableDefinitionNode>(),
                new List<DirectiveNode>(),
                selectionSet);
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
    }
}
