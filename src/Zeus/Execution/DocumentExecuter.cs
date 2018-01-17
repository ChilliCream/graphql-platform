using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser.AST;

namespace Zeus.Execution
{
    public class DocumentExecuter
        : IDocumentExecuter
    {
        //schema, document, operationName, variableValues, initialValue
        public async Task<IDictionary<string, object>> ExecuteAsync(
            ISchema schema, IDocument document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {
            GraphQLOperationDefinition operation = document.GetOperation(operationName);
            List<QueryContext> level = new List<QueryContext>();

            foreach (GraphQLFieldSelection fieldSelection in operation.SelectionSet.Selections)
            {
                level.Add(new QueryContext("Query", fieldSelection, new ResolverContext(ImmutableStack<object>.Empty)));
            }

            List<QueryContext> nextLevel = level;
            do
            {
                nextLevel = new List<QueryContext>(await ExecuteLevelAsync(schema, variables, nextLevel, cancellationToken));
            }
            while (nextLevel.Any());

            return null;
        }

        private async Task<IEnumerable<QueryContext>> ExecuteLevelAsync(Schema schema, IDictionary<string, object> variables,
            IEnumerable<QueryContext> items, CancellationToken cancellationToken)
        {

            // resolve => this could be done in parallel
            foreach (QueryContext queryContext in items)
            {
                queryContext.ResolverResult = await ResolveAsync(schema,
                    queryContext.TypeName, queryContext.FieldSelection,
                    variables, cancellationToken);
            }

            // execute batches => this could  be done in parallel
            // ExecuteBatches

            return ProcessResolverResults(items);
        }

        private IEnumerable<QueryContext> ProcessResolverResults(IEnumerable<QueryContext> queryContexts)
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
                    nextLevel.AddRange(HandleObjectResults(queryContext, fieldName));
                }
                else
                {
                    nextLevel.AddRange(HandleListResult(queryContext, fieldName));
                }
            }

            return nextLevel;
        }

        private IEnumerable<QueryContext> HandleListResult(QueryContext queryContext, string fieldName)
        {
            TypeDeclaration type = queryContext.ResolverResult.Field.Type;
            object result = queryContext.ResolverResult.Result;

            if (type.ElementType.Kind == TypeKind.Object && result is IEnumerable)
            {
                return HandleObjectListResults(queryContext, fieldName);
            }

            if (type.ElementType.Kind == TypeKind.Scalar && result is IEnumerable)
            {
                return HandleScalarListResults(queryContext, fieldName);
            }

            if (type.ElementType.Kind == TypeKind.Object)
            {
                return HandleSingleObjectListResults(queryContext, fieldName);
            }

            return HandleSingleScalarListResults(queryContext, fieldName);
        }

        private IEnumerable<QueryContext> HandleObjectListResults(QueryContext queryContext, string fieldName)
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
                    queryContext.ResolverContext, map, element))
                {
                    yield return nextContext;
                }
            }
        }

        private IEnumerable<QueryContext> HandleSingleObjectListResults(QueryContext queryContext, string fieldName)
        {
            Dictionary<string, object>[] list = new Dictionary<string, object>[] { new Dictionary<string, object>() };
            queryContext.Response[fieldName] = list;
            object element = queryContext.ResolverResult.Result;

            return CreateChildContexts(
                queryContext.FieldSelection.SelectionSet.Selections.OfType<GraphQLFieldSelection>(),
                queryContext.ResolverResult.Field.Type.ElementType.Name,
                queryContext.ResolverContext, list.First(), element);
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

        private IEnumerable<QueryContext> HandleObjectResults(QueryContext queryContext, string fieldName)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            queryContext.Response[fieldName] = map;

            return CreateChildContexts(
                queryContext.FieldSelection.SelectionSet.Selections.OfType<GraphQLFieldSelection>(),
                queryContext.ResolverResult.Field.Type.Name,
                queryContext.ResolverContext, map,
                queryContext.ResolverResult.Result);
        }

        private IEnumerable<QueryContext> HandleScalarResults(QueryContext queryContext, string fieldName)
        {
            queryContext.Response[fieldName] = queryContext.ResolverResult.Result;
            yield break;
        }

        private IEnumerable<QueryContext> CreateChildContexts(IEnumerable<GraphQLFieldSelection> selections,
            string typeName, IResolverContext parentContext, IDictionary<string, object> map, object obj)
        {
            foreach (GraphQLFieldSelection fieldSelection in selections)
            {
                yield return new QueryContext(typeName, fieldSelection,
                    new ResolverContext(parentContext.Path.Push(obj)),
                    map);
            }
        }


        private async Task<ResolverResult> ResolveAsync(Schema schema, string typeName,
            GraphQLFieldSelection fieldSelection, IDictionary<string, object> variables,
            CancellationToken cancellationToken)
        {
            if (schema.TryGetObjectType(typeName, out ObjectDeclaration type))
            {
                if (type.Fields.TryGetValue(fieldSelection.Name.Value, out FieldDeclaration field))
                {
                    if (schema.TryGetResolver(typeName, fieldSelection.Name.Value, out var resolver))
                    {
                        object result = await resolver.ResolveAsync(null, cancellationToken);
                        return new ResolverResult(typeName, field, result);
                    }
                }
            }

            throw new Exception("TODO: Error Handling");
        }
    }
}