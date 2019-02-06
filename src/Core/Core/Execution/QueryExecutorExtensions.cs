using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class QueryExecutorExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            QueryRequest request)
        {
            return executor.ExecuteAsync(
                request.ToReadOnly(),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query).ToReadOnly(),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecutor executor,
            string query,
            CancellationToken cancellationToken)
        {
            return executor.ExecuteAsync(
                new QueryRequest(query).ToReadOnly(),
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
                }.ToReadOnly(),
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
                }.ToReadOnly(),
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
    }
}
