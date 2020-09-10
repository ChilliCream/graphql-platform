using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public static class ExecutionRequestExecutorExtensions
    {
        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            IReadOnlyQueryRequest request)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return executor.ExecuteAsync(
                request,
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            string query)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            return executor.ExecuteAsync(
                QueryRequestBuilder.New().SetQuery(query).Create(),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            string query,
            CancellationToken cancellationToken)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            return executor.ExecuteAsync(
                QueryRequestBuilder.New().SetQuery(query).Create(),
                cancellationToken);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            string query,
            IReadOnlyDictionary<string, object?> variableValues)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            if (variableValues is null)
            {
                throw new ArgumentNullException(nameof(variableValues));
            }

            return executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValues(variableValues)
                    .Create(),
                CancellationToken.None);
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            string query,
            IReadOnlyDictionary<string, object?> variableValues,
            CancellationToken cancellationToken)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            if (variableValues is null)
            {
                throw new ArgumentNullException(nameof(variableValues));
            }

            return executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValues(variableValues)
                    .Create(),
                cancellationToken);
        }

        public static IExecutionResult Execute(
            this IRequestExecutor executor,
            IReadOnlyQueryRequest request)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return Task.Factory.StartNew(
                () => ExecuteAsync(executor, request))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static IExecutionResult Execute(
            this IRequestExecutor executor,
            string query)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            return executor.Execute(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create());
        }

        public static IExecutionResult Execute(
            this IRequestExecutor executor,
            string query,
            IReadOnlyDictionary<string, object?> variableValues)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query mustn't be null or empty.",
                    nameof(query));
            }

            if (variableValues is null)
            {
                throw new ArgumentNullException(nameof(variableValues));
            }

            return executor.Execute(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValues(variableValues)
                    .Create());
        }

        public static Task<IExecutionResult> ExecuteAsync(
            this IRequestExecutor executor,
            Action<IQueryRequestBuilder> buildRequest,
            CancellationToken cancellationToken)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (buildRequest is null)
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
            this IRequestExecutor executor,
            Action<IQueryRequestBuilder> buildRequest)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (buildRequest is null)
            {
                throw new ArgumentNullException(nameof(buildRequest));
            }

            return executor.ExecuteAsync(
                buildRequest,
                CancellationToken.None);
        }
    }
}
