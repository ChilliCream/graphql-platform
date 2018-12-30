using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class QueryExecuterExtensions
    {
        [Obsolete("Use MakeExecutable instead.")]
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            QueryRequest request)
        {
            return executer.ExecuteAsync(
                request,
                CancellationToken.None);
        }

        [Obsolete("Use MakeExecutable instead.")]
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Obsolete("Use MakeExecutable instead.")]
        public static Task<IExecutionResult> ExecuteAsync(
            this IQueryExecuter executer,
            string query,
            CancellationToken cancellationToken)
        {
            return executer.ExecuteAsync(
                new QueryRequest(query),
                cancellationToken);
        }

        [Obsolete("Use MakeExecutable instead.")]
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

        [Obsolete("Use MakeExecutable instead.")]
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

        [Obsolete("Use MakeExecutable instead.")]
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

        [Obsolete("Use MakeExecutable instead.")]
        public static IExecutionResult Execute(
            this IQueryExecuter executer,
            string query)
        {
            return executer.Execute(new QueryRequest(query));
        }

        [Obsolete("Use MakeExecutable instead.")]
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
