using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GraphQLParser.AST;
using Zeus.Resolvers;
using Zeus.Types;

namespace Zeus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        private readonly IServiceProvider _serviceProvider;

        public DocumentExecuter()
            : this(new DefaultServiceProvider())
        { }

        public DocumentExecuter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IDictionary<string, object>> ExecuteAsync(
            ISchema schema, IDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {
            GraphQLOperationDefinition operation = document
                .GetOperation(operationName);

            List<QueryContext> root = new List<QueryContext>();
            foreach (GraphQLFieldSelection fieldSelection
                     in operation.SelectionSet.Selections)
            {
                ResolverContext resolverContext = ResolverContext.Create(
                    _serviceProvider, GetArguments(fieldSelection, variables),
                    initialValue);

                root.Add(new QueryContext(operation.Operation.ToString(),
                    fieldSelection, resolverContext));
            }

            IEnumerable<QueryContext> nextLevel = root;
            do
            {
                nextLevel = new List<QueryContext>(
                    await ExecuteLevelAsync(schema,
                        variables, nextLevel, cancellationToken));
            }
            while (nextLevel.Any());

            // todo: rework this
            return root.First().Response;
        }

        private Dictionary<string, object> GetArguments(
            GraphQLFieldSelection fieldSelection,
            IDictionary<string, object> variables)
        {
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            foreach (GraphQLArgument argument in fieldSelection.Arguments)
            {
                switch (argument.Value)
                {
                    case GraphQLVariable v:
                        arguments[argument.Name.Value] = variables[v.Name.Value];
                        break;
                    default:
                        break;
                }
            }
            return arguments;
        }

        private async Task<IEnumerable<QueryContext>> ExecuteLevelAsync(
            ISchema schema,
            IDictionary<string, object> variables,
            IEnumerable<QueryContext> items,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(items.Select(t => new Func<Task>(
                async () => t.ResolverResult = await ResolveAsync(
                        schema, t.TypeName, t.FieldSelection,
                        t.ResolverContext, variables,
                        cancellationToken)))
                .Select(t => t()));

            return ProcessResolverResults(items, variables);
        }

        private IEnumerable<QueryContext> ProcessResolverResults(
            IEnumerable<QueryContext> queryContexts,
            IDictionary<string, object> variables)
        {
            List<QueryContext> nextLevel = new List<QueryContext>();

            foreach (QueryContext queryContext in queryContexts)
            {
                string fieldName = queryContext.FieldSelection.Alias == null
                    ? queryContext.FieldSelection.Name.Value
                    : queryContext.FieldSelection.Alias.Value;
                TypeDeclaration type = queryContext.ResolverResult.Field.Type;
                object result = queryContext.ResolverResult.Result;

                queryContext.ResolverResult.FinalizeResult();

                if (result == null)
                {
                    queryContext.Response[fieldName] = null;
                }
                else if (type.Kind == TypeKind.Scalar)
                {
                    nextLevel.AddRange(HandleScalarResults(queryContext, fieldName));
                }
                else if (type.Kind == TypeKind.Object)
                {
                    nextLevel.AddRange(HandleObjectResults(queryContext, fieldName, variables));
                }
                else
                {
                    nextLevel.AddRange(HandleListResult(queryContext, fieldName, variables));
                }
            }

            return nextLevel;
        }

        private IEnumerable<QueryContext> HandleListResult(
            QueryContext queryContext, string fieldName,
            IDictionary<string, object> variables)
        {
            TypeDeclaration type = queryContext.ResolverResult.Field.Type;
            object result = queryContext.ResolverResult.Result;

            if (type.ElementType.Kind == TypeKind.Object && result is IEnumerable)
            {
                return HandleObjectListResults(queryContext, fieldName, variables);
            }

            if (type.ElementType.Kind == TypeKind.Scalar && result is IEnumerable)
            {
                return HandleScalarListResults(queryContext, fieldName);
            }

            if (type.ElementType.Kind == TypeKind.Object)
            {
                return HandleSingleObjectListResults(queryContext, fieldName, variables);
            }

            return HandleSingleScalarListResults(queryContext, fieldName);
        }

        private IEnumerable<QueryContext> HandleObjectListResults(
            QueryContext queryContext, string fieldName,
            IDictionary<string, object> variables)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            queryContext.Response[fieldName] = list;

            foreach (object element in (IEnumerable)queryContext.ResolverResult.Result)
            {
                Dictionary<string, object> map = new Dictionary<string, object>();
                list.Add(map);

                foreach (QueryContext nextContext in CreateChildContexts(
                    queryContext.FieldSelection.SelectionSet.Selections.OfType<GraphQLFieldSelection>(),
                    queryContext.ResolverResult.Field.Type.ElementType.Name,
                    queryContext.ResolverContext, map, element, variables))
                {
                    yield return nextContext;
                }
            }
        }

        private IEnumerable<QueryContext> HandleSingleObjectListResults(
            QueryContext queryContext, string fieldName,
            IDictionary<string, object> variables)
        {
            Dictionary<string, object>[] list = new Dictionary<string, object>[] { new Dictionary<string, object>() };
            queryContext.Response[fieldName] = list;
            object element = queryContext.ResolverResult.Result;

            return CreateChildContexts(
                queryContext.FieldSelection.SelectionSet.Selections.OfType<GraphQLFieldSelection>(),
                queryContext.ResolverResult.Field.Type.ElementType.Name,
                queryContext.ResolverContext, list.First(), element, variables);
        }

        private IEnumerable<QueryContext> HandleScalarListResults(QueryContext queryContext, string fieldName)
        {
            queryContext.Response[fieldName] = ((IEnumerable)queryContext.ResolverResult.Result).Cast<object>().ToArray();
            yield break;
        }

        private IEnumerable<QueryContext> HandleSingleScalarListResults(QueryContext queryContext, string fieldName)
        {
            queryContext.Response[fieldName] = new[] { queryContext.ResolverResult.Result };
            yield break;
        }

        private IEnumerable<QueryContext> HandleObjectResults(
            QueryContext queryContext, string fieldName,
            IDictionary<string, object> variables)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            queryContext.Response[fieldName] = map;

            return CreateChildContexts(
                queryContext.FieldSelection.SelectionSet.Selections.OfType<GraphQLFieldSelection>(),
                queryContext.ResolverResult.Field.Type.Name,
                queryContext.ResolverContext, map,
                queryContext.ResolverResult.Result, variables);
        }

        private IEnumerable<QueryContext> HandleScalarResults(QueryContext queryContext, string fieldName)
        {
            queryContext.Response[fieldName] = queryContext.ResolverResult.Result;
            yield break;
        }

        private IEnumerable<QueryContext> CreateChildContexts(
            IEnumerable<GraphQLFieldSelection> selections,
            string typeName, IResolverContext parentContext,
            IDictionary<string, object> map, object obj,
            IDictionary<string, object> variables)
        {
            foreach (GraphQLFieldSelection fieldSelection in selections)
            {
                yield return new QueryContext(typeName, fieldSelection,
                    parentContext.Copy(GetArguments(fieldSelection, variables), obj),
                    map);
            }
        }


        private async Task<ResolverResult> ResolveAsync(ISchema schema, string typeName,
            GraphQLFieldSelection fieldSelection, IResolverContext context,
            IDictionary<string, object> variables, CancellationToken cancellationToken)
        {
            if (schema.TryGetObjectType(typeName, out ObjectDeclaration type))
            {
                if (type.Fields.TryGetValue(fieldSelection.Name.Value,
                    out FieldDeclaration field))
                {
                    IResolver resolver = GetResolver(schema, typeName,
                        fieldSelection.Name.Value, () => context.Parent<object>());
                    object result = await resolver.ResolveAsync(context, cancellationToken);
                    return new ResolverResult(typeName, field, result);
                }
            }

            throw new Exception("TODO: Error Handling");
        }

        private IResolver GetResolver(ISchema schema, string typeName, string fieldName, Func<object> parent)
        {
            IResolver resolver;
            if (schema.Resolvers.TryGetResolver(_serviceProvider,
                typeName, fieldName, out resolver))
            {
                return resolver;
            }

            object obj = parent();
            if (obj != null && schema.Resolvers.TryGetResolver(_serviceProvider,
                obj.GetType(), fieldName, out resolver))
            {
                return resolver;
            }

            throw new Exception("TODO: Could not find a resolver .... ");
        }
    }
}