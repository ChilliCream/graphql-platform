using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    // TODO : argument validation!!!
    public static class QueryExecutorExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            IReadOnlyQueryRequest request)
        {
            return executor.ExecuteAsync(
                request,
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query,
            CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query),
                cancellationToken);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query,
            IReadOnlyDictionary<string, object> variableValues)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                },
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                },
                cancellationToken);
        }

        public static IExecutionResult Execute(
            this IQueryExecutor executor,
            QueryRequest request)
        {
            return Task.Factory.StartNew(
                () => ExecuteAsync(executor, request))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static IExecutionResult Execute(
            this IQueryExecutor executor,
            string query)
        {
            return executor.Execute(new QueryRequest(query));
        }

        public static IExecutionResult Execute(
            this IQueryExecutor executor,
            string query,
            IReadOnlyDictionary<string, object> variableValues)
        {
            return executor.Execute(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                });
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            Action<IQueryRequestBuilder> buildRequest,
            CancellationToken cancellationToken)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (buildRequest == null)
            {
                throw new ArgumentNullException(nameof(buildRequest));
            }

            var builder = new QueryRequestBuilder();
            buildRequest(builder);

            return executor.ExecuteAsync(
                builder.Create(),
                cancellationToken);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            Action<IQueryRequestBuilder> buildRequest)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (buildRequest == null)
            {
                throw new ArgumentNullException(nameof(buildRequest));
            }

            return executor.ExecuteAsync(
                buildRequest,
                CancellationToken.None);
        }
    }
}
