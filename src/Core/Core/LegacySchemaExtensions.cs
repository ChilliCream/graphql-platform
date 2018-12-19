using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate
{
    // TODO : make obsolete
    public static class LegacySchemaExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return ExecuteAsync(schema, request, cancellationToken);
        }

        public static async Task<IExecutionResult> ExecuteAsync(
           this ISchema schema, QueryRequest request,
           CancellationToken cancellationToken = default)
        {
            using (IQueryExecuter executer = QueryExecutionBuilder.New()
                .UseDefaultPipeline().Build(schema))
            {
                return await executer.ExecuteAsync(request, cancellationToken);
            }
        }

        public static IExecutionResult Execute(
            this ISchema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static IExecutionResult Execute(
            this ISchema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        public static IExecutionResult Execute(
            this ISchema schema, string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        public static IExecutionResult Execute(
            this ISchema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return Execute(schema, request, cancellationToken);
        }

        public static IExecutionResult Execute(
            this ISchema schema, QueryRequest request,
           CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(
                () => ExecuteAsync(schema, request, cancellationToken))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }
    }
}
