using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Provides extensions for <see cref="IServiceProvider" /> to interact with the 
    /// <see cref="IRequestExecutor" />.
    /// </summary>
    public static class RequestExecutorServiceProviderExtensions
    {
        /// <summary>
        /// Gets the <see cref="IRequestExecutor" /> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceProvider"/>.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutor" />.
        /// </returns>
        public static async ValueTask<ISchema> GetSchemaAsync(
            this IServiceProvider services,
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            IRequestExecutor executor = 
                await GetRequestExecutorAsync(services, schemaName, cancellationToken)
                    .ConfigureAwait(false);

            return executor.Schema;
        }

        /// <summary>
        /// Gets the <see cref="IRequestExecutor" /> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceProvider"/>.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutor" />.
        /// </returns>
        public static ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            this IServiceProvider services,
            NameString schemaName = default,
            CancellationToken cancellationToken = default) =>
            services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync(schemaName, cancellationToken);

        /// <summary>
        /// Executes the given GraphQL <paramref name="request" />.
        /// </summary>
        /// <param name="services">
        /// The service provider that contains the executor.
        /// </param>
        /// <param name="request">
        /// The GraphQL request object.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the execution result of the given GraphQL <paramref name="request" />.
        ///
        /// If the request operation is a simple query or mutation the result is a
        /// <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a query or mutation where data is deferred, streamed or
        /// includes live data the result is a <see cref="global::HotChocolate.Execution.IResponseStream" /> where each result
        /// that the <see cref="global::HotChocolate.Execution.IResponseStream" /> yields is a <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a subscription the result is a
        /// <see cref="global::HotChocolate.Execution.IResponseStream" /> where each result that the
        /// <see cref="global::HotChocolate.Execution.IResponseStream" /> yields is a
        /// <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        /// </returns>
        public static async Task<IExecutionResult> ExecuteRequestAsync(
            this IServiceProvider services,
            IQueryRequest request,
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            IRequestExecutor executor =
                await GetRequestExecutorAsync(services, schemaName, cancellationToken)
                    .ConfigureAwait(false);

            return await executor.ExecuteAsync(request, cancellationToken);
        }

        /// <summary>
        /// Executes the given GraphQL <paramref name="request" />.
        /// </summary>
        /// <param name="services">
        /// The service provider that contains the executor.
        /// </param>
        /// <param name="request">
        /// The GraphQL request object.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns the execution result of the given GraphQL <paramref name="request" />.
        ///
        /// If the request operation is a simple query or mutation the result is a
        /// <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a query or mutation where data is deferred, streamed or
        /// includes live data the result is a <see cref="global::HotChocolate.Execution.IResponseStream" /> where each result
        /// that the <see cref="global::HotChocolate.Execution.IResponseStream" /> yields is a <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        ///
        /// If the request operation is a subscription the result is a
        /// <see cref="global::HotChocolate.Execution.IResponseStream" /> where each result that the
        /// <see cref="global::HotChocolate.Execution.IResponseStream" /> yields is a
        /// <see cref="global::HotChocolate.Execution.IReadOnlyQueryResult" />.
        /// </returns>
        public static async Task<IExecutionResult> ExecuteRequestAsync(
            this IServiceProvider services,
            string query,
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            IRequestExecutor executor =
                await GetRequestExecutorAsync(services, schemaName, cancellationToken)
                    .ConfigureAwait(false);

            return await executor.ExecuteAsync(query, cancellationToken);
        }

        /// <summary>
        /// Executes the given GraphQL <paramref name="requestBatch" />.
        /// </summary>
        /// <param name="services">
        /// The service provider that contains the executor.
        /// </param>
        /// <param name="requestBatch">
        /// The GraphQL request batch.
        /// </param>
        /// <param name="allowParallelExecution">
        /// Defines if the executor is allowed to execute the batch in parallel.
        /// </param>
        /// <param name="schemaName">
        /// The schema name.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Returns a stream of query results.
        /// </returns>
        public static async Task<IBatchQueryResult> ExecuteBatchRequestAsync(
            this IServiceProvider services,
            IEnumerable<IQueryRequest> requestBatch,
            bool allowParallelExecution = false,
            NameString schemaName = default,
            CancellationToken cancellationToken = default)
        {
            IRequestExecutor executor =
                await GetRequestExecutorAsync(services, schemaName, cancellationToken)
                    .ConfigureAwait(false);

            return await executor.ExecuteBatchAsync(
                requestBatch, allowParallelExecution, cancellationToken);
        }
    }
}