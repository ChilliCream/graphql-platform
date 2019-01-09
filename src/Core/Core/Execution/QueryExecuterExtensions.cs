using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class QueryExecuterExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            QueryRequest request)
        {
            return executer.ExecuteAsync(
                request,
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query,
            CancellationToken cancellationToken)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query),
                cancellationToken);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query,
            IReadOnlyDictionary<string, object> variableValues)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                },
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                },
                cancellationToken);
        }

        public static IExecutionResult Execute(
            this IQueryExecuter executer,
            QueryRequest request)
        {
            return Task.Factory.StartNew(
                () => ExecuteAsync(executer, request))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static IExecutionResult Execute(
            this IQueryExecuter executer,
            string query)
        {
            return executer.Execute(new QueryRequest(query));
        }

        public static IExecutionResult Execute(
            this IQueryExecuter executer,
            string query,
            IReadOnlyDictionary<string, object> variableValues)
        {
            return executer.Execute(
                new QueryRequest(query)
                {
                    VariableValues = variableValues
                });
        }
    }
}
