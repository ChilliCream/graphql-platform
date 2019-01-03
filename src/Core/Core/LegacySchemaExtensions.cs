using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate
{
    public static class LegacySchemaExtensions
    {
        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static Task<IExecutionResult> ExecuteAsync(
            this ISchema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return ExecuteAsync(schema, request, cancellationToken);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
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

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static IExecutionResult Execute(
            this ISchema schema, string query,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static IExecutionResult Execute(
            this ISchema schema, string query, string operationName,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query, operationName),
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static IExecutionResult Execute(
            this ISchema schema, string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken = default)
        {
            return Execute(schema,
                new QueryRequest(query) { VariableValues = variableValues },
                CancellationToken.None);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
        public static IExecutionResult Execute(
            this ISchema schema, string query,
            Action<QueryRequest> configure,
            CancellationToken cancellationToken = default)
        {
            QueryRequest request = new QueryRequest(query);
            configure?.Invoke(request);
            return Execute(schema, request, cancellationToken);
        }

        [Obsolete(
            "Use schema.MakeExecutable(). " +
            "This method will be removed with version 1.0.0.",
            true)]
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
