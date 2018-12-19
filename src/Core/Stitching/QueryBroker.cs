
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal class QueryBroker
        : IQueryBroker
    {
        public Task<IExecutionResult> RedirectQueryAsync(
            IDirectiveContext directiveContext)
        {
            if (directiveContext == null)
            {
                throw new ArgumentNullException(nameof(directiveContext));
            }

            string schemaName = directiveContext.FieldSelection.GetSchemaName();
            var stitchingCtx = directiveContext.Service<IStitchingContext>();

            IQueryExecuter queryExecuter =
                stitchingCtx.GetQueryExecuter(schemaName);

            QueryRequest queryRequest = CreateQuery(directiveContext);

            return queryExecuter.ExecuteAsync(
                queryRequest,
                directiveContext.RequestAborted);
        }

        private QueryRequest CreateQuery(
            IDirectiveContext directiveContext)
        {
            var rewriter = new ExtractRemoteQueryRewriter();
            var fieldSelection = (FieldNode)rewriter.Rewrite(
                directiveContext.FieldSelection,
                directiveContext.FieldSelection.GetSchemaName());

            OperationType operation =
                GetOperationType(directiveContext);

            Stack<SelectionPathComponent> selectionPath =
                GetSelectionPath(directiveContext);

            IReadOnlyDictionary<string, object> variables =
                CreateVariables(directiveContext, selectionPath);

            DocumentNode query = CreateDelegationQuery(
                operation, selectionPath, fieldSelection);

            return new QueryRequest(SerializeQuery(query))
            {
                VariableValues = variables
            };
        }

        private DocumentNode CreateDelegationQuery(
            OperationType operation,
            Stack<SelectionPathComponent> path,
            FieldNode selection)
        {
            FieldNode current = selection;

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
                    current.SelectionSet,
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

        private OperationType GetOperationType(
            IDirectiveContext directiveContext)
        {
            var directive = directiveContext.Directive
                .ToObject<DelegateDirective>();

            if (!Enum.TryParse(directive.Operation, out OperationType type))
            {
                type = OperationType.Query;
            }

            return type;
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

        private static string SerializeQuery(DocumentNode query)
        {
            var queryText = new StringBuilder();

            using (var stringWriter = new StringWriter(queryText))
            {
                var serializer = new QuerySyntaxSerializer();
                serializer.Visit(query, new DocumentWriter(stringWriter));
            }

            return queryText.ToString();
        }

        // TODO : rework and finalize
        private static IReadOnlyDictionary<string, object> CreateVariables(
            IDirectiveContext directiveContext,
            IEnumerable<SelectionPathComponent> components)
        {
            var root = new Dictionary<string, object>();

            foreach (var component in components)
            {
                foreach (ArgumentNode argument in component.Arguments)
                {
                    if (argument.Value is ScopedVariableNode sv)
                    {
                        switch (sv.Scope.Value)
                        {
                            case "arguments":
                                root[sv.ToVariableName()] =
                                    directiveContext.Argument<object>(
                                        sv.Name.Value);
                                break;
                            case "variables":
                                break;
                            case "properties":
                                root[sv.ToVariableName()] = directiveContext
                                    .Parent<IDictionary<string, object>>()
                                        [sv.Name.Value];
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            return root;
        }

    }
}
