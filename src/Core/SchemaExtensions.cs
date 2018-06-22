using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate
{
    public static class SchemaExtensions
    {
        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query,
            IReadOnlyDictionary<string, IValueNode> variableValues,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        public static Task<QueryResult> ExecuteAsync(
            this Schema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return ExecuteAsync(schema, request, cancellationToken);
        }

        public static async Task<QueryResult> ExecuteAsync(
           this Schema schema, QueryRequest request,
           CancellationToken cancellationToken = default)
        {
            QueryExecuter executer = new QueryExecuter(schema, 0);
            return await executer.ExecuteAsync(request, cancellationToken);
        }

        public static QueryResult Execute(
            this Schema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static QueryResult Execute(
            this Schema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        public static QueryResult Execute(
            this Schema schema, string query,
            IReadOnlyDictionary<string, IValueNode> variableValues,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        public static QueryResult Execute(
            this Schema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return Execute(schema, request, cancellationToken);
        }

        public static QueryResult Execute(
            this Schema schema, QueryRequest request,
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
