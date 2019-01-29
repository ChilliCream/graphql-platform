using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static readonly RootScopedVariableResolver _resolvers =
            new RootScopedVariableResolver();
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
                IImmutableStack<SelectionPathComponent> path =
                    delegateDirective.Path is null
                    ? ImmutableStack<SelectionPathComponent>.Empty
                    : SelectionPathParser.Parse(delegateDirective.Path);

                QueryRequest request = CreateQuery(context, path);

                IReadOnlyQueryResult result = await ExecuteQueryAsync(
                    context, request, schemaDirective.Name)
                    .ConfigureAwait(false);

                context.Result = ExtractData(result.Data, path.Count());
                ReportErrors(context, result.Errors);
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }

        private static QueryRequest CreateQuery(
            IMiddlewareContext context,
            IImmutableStack<SelectionPathComponent> path)
        {
            var fieldRewriter = new ExtractFieldQuerySyntaxRewriter(
                context.Schema);

            ExtractedField extractedField = fieldRewriter.ExtractField(
                    context.QueryDocument, context.Operation,
                    context.FieldSelection, context.ObjectType);

            IReadOnlyCollection<VariableValue> variableValues =
                CreateVariableValues(context, path, extractedField);

            DocumentNode query = RemoteQueryBuilder.New()
                .SetOperation(context.Operation.Operation)
                .SetSelectionPath(path)
                .SetRequestField(extractedField.Field)
                .AddVariables(CreateVariableDefs(variableValues))
                .AddFragmentDefinitions(extractedField.Fragments)
                .Build();

            return new QueryRequest(QuerySyntaxSerializer.Serialize(query))
            {
                VariableValues = CreateVariables(variableValues),
                Services = context.Service<IServiceProvider>()
            };
        }

        private static async Task<IReadOnlyQueryResult> ExecuteQueryAsync(
            IResolverContext context,
            QueryRequest request,
            string schemaName)
        {
            IQueryExecutor remoteExecutor =
                context.Service<IStitchingContext>()
                    .GetQueryExecutor(schemaName);

            IExecutionResult result =
                await remoteExecutor.ExecuteAsync(request)
                    .ConfigureAwait(false);

            if (result is IReadOnlyQueryResult queryResult)
            {
                return queryResult;
            }

            throw new QueryException(
                "Only query results are supported in the " +
                "delegation middleware.");
        }

        private static object ExtractData(
            IReadOnlyDictionary<string, object> data,
            int levels)
        {
            if (data.Count == 0)
            {
                return null;
            }

            object obj = data.Count == 0 ? null : data.First().Value;

            if (obj != null && levels > 1)
            {
                for (int i = levels - 1; i >= 1; i--)
                {
                    var current = obj as IReadOnlyDictionary<string, object>;
                    obj = current.Count == 0 ? null : current.First().Value;
                    if (obj is null)
                    {
                        return null;
                    }
                }
            }

            return obj;
        }

        private static void ReportErrors(
            IResolverContext context,
            IEnumerable<IError> errors)
        {
            IReadOnlyCollection<object> path = context.Path.ToCollection();

            foreach (IError error in errors)
            {
                context.ReportError(error.AddExtension("remote", path));
            }
        }

        private static IReadOnlyCollection<VariableValue> CreateVariableValues(
            IMiddlewareContext context,
            IEnumerable<SelectionPathComponent> components,
            ExtractedField extractedField)
        {
            var values = new Dictionary<string, VariableValue>();

            IReadOnlyDictionary<string, object> requestVariables =
                context.GetVariables();

            foreach (VariableValue value in ResolveScopedVariables(
                context, components))
            {
                values[value.Name] = value;
            }

            foreach (VariableValue value in ResolveUsedRequestVariables(
                extractedField, requestVariables))
            {
                values[value.Name] = value;
            }

            return values.Values;
        }

        private static IEnumerable<VariableValue> ResolveScopedVariables(
            IResolverContext context,
            IEnumerable<SelectionPathComponent> components)
        {
            foreach (var component in components)
            {
                foreach (ArgumentNode argument in component.Arguments)
                {
                    if (argument.Value is ScopedVariableNode sv)
                    {
                        yield return _resolvers.Resolve(context, sv);
                    }
                }
            }
        }

        private static IEnumerable<VariableValue> ResolveUsedRequestVariables(
            ExtractedField extractedField,
            IReadOnlyDictionary<string, object> requestVariables)
        {
            foreach (VariableDefinitionNode variable in
                extractedField.Variables)
            {
                string name = variable.Variable.Name.Value;
                requestVariables.TryGetValue(name, out object value);

                yield return new VariableValue
                (
                    name,
                    variable.Type,
                    value,
                    variable.DefaultValue
                );
            }
        }

        private static IReadOnlyDictionary<string, object> CreateVariables(
            IEnumerable<VariableValue> variableValues)
        {
            return variableValues.ToDictionary(t => t.Name, t => t.Value);
        }

        private static IReadOnlyList<VariableDefinitionNode> CreateVariableDefs(
            IReadOnlyCollection<VariableValue> variableValues)
        {
            var definitions = new List<VariableDefinitionNode>();

            foreach (VariableValue variableValue in variableValues)
            {
                definitions.Add(new VariableDefinitionNode(
                    null,
                    new VariableNode(new NameNode(variableValue.Name)),
                    variableValue.Type,
                    variableValue.DefaultValue
                ));
            }

            return definitions;
        }
    }

}
