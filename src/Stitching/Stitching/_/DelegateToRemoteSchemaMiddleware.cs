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
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private readonly FieldDelegate _next;

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            DelegateDirective delegateDirective = context.Field
                .Directives[DirectiveNames.Delegate]
                .FirstOrDefault()?.ToObject<DelegateDirective>();

            SchemaDirective schemaDirective = context.Field
                .Directives[DirectiveNames.Schema]
                .FirstOrDefault()?.ToObject<SchemaDirective>();

            if (delegateDirective != null && schemaDirective != null)
            {
                Stack<SelectionPathComponent> path =
                    delegateDirective.Path is null
                    ? new Stack<SelectionPathComponent>()
                    : SelectionPathParser.Parse(delegateDirective.Path);
                int levels = path.Count;

                var fieldRewriter = new ExtractFieldQuerySyntaxRewriter(
                    context.Schema);

                ExtractedField extractedField = fieldRewriter.ExtractField(
                    context.QueryDocument, context.Operation,
                    context.FieldSelection, context.ObjectType);

                DocumentNode query = RemoteQueryBuilder.New()
                    .SetOperation(context.Operation.Operation)
                    .SetSelectionPath(path)
                    .SetRequestField(extractedField.Field)
                    .AddVariables(extractedField.Variables)
                    .AddFragmentDefinitions(extractedField.Fragments)
                    .Build();

                var queryText = new StringBuilder();
                var writer = new DocumentWriter(new StringWriter(queryText));
                var serializer = new QuerySyntaxSerializer();
                serializer.Visit(query, writer);

                var request = new QueryRequest(queryText.ToString());
                request.VariableValues =
                    CreateVariables(context, path, extractedField);
                request.Services = context.Service<IServiceProvider>();

                IQueryExecutor remoteExecutor =
                    context.Service<IStitchingContext>()
                        .GetQueryExecutor(schemaDirective.Name);

                IExecutionResult result =
                    await remoteExecutor.ExecuteAsync(request)
                        .ConfigureAwait(false);

                if (result is IReadOnlyQueryResult queryResult)
                {
                    context.Result = ExtractData(queryResult.Data, levels);
                    ReportErrors(context, queryResult.Errors);
                }
                else
                {
                    throw new QueryException(
                        "Only query results are supported in the " +
                        "delegation middleware.");
                }
            }

            await _next.Invoke(context);
        }

        private IReadOnlyDictionary<string, object> ExtractData(
            IReadOnlyDictionary<string, object> data,
            int levels)
        {
            if (levels > 1)
            {
                IReadOnlyDictionary<string, object> current = data;

                for (int i = levels; i >= 1; i--)
                {
                    object obj = data.Count == 0 ? null : data.First().Value;
                    if (obj is IReadOnlyDictionary<string, object> next)
                    {
                        current = next;
                    }
                    else
                    {
                        return null;
                    }
                }

                return current;
            }

            return data;
        }

        private void ReportErrors(
            IMiddlewareContext context,
            IEnumerable<IError> errors)
        {
            IReadOnlyCollection<string> path = context.Path.ToCollection();

            foreach (IError error in errors)
            {
                context.ReportError(error.AddExtension("remote", path));
            }
        }

        private static IReadOnlyDictionary<string, object> CreateVariables(
            IMiddlewareContext context,
            IEnumerable<SelectionPathComponent> components,
            ExtractedField extractedField)
        {
            var root = new Dictionary<string, object>();

            IReadOnlyDictionary<string, object> variables =
                context.GetVariables();

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
                                    context.Argument<object>(
                                        sv.Name.Value);
                                break;
                            case "variables":
                                root[sv.ToVariableName()] =
                                    variables[sv.Name.Value];
                                break;
                            case "properties":
                                root[sv.ToVariableName()] = context
                                    .Parent<IReadOnlyDictionary<string, object>>()
                                        [sv.Name.Value];
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            foreach (VariableDefinitionNode variable in
                extractedField.Variables)
            {
                root[variable.Variable.Name.Value] =
                    variables[variable.Variable.Name.Value];
            }

            return root;
        }
    }
}
