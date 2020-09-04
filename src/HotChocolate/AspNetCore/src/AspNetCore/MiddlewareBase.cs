using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Language;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class MiddlewareBase : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly RequestDelegate _next;
        private readonly IRequestExecutorResolver _executorResolver;
        private readonly IHttpResultSerializer _resultSerializer;
        private IRequestExecutor? _executor;
        private bool _disposed;

        protected MiddlewareBase(
            RequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _executorResolver = executorResolver ??
                throw new ArgumentNullException(nameof(executorResolver));
            _resultSerializer = resultSerializer ??
                throw new ArgumentNullException(nameof(executorResolver));
            SchemaName = schemaName;

            executorResolver.RequestExecutorEvicted += EvictRequestExecutor;
        }

        /// <summary>
        /// Gets the name of the schema that this middleware serves up.
        /// </summary>
        protected NameString SchemaName { get; }

        /// <summary>
        /// Invokes the next middleware in line.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/>.
        /// </param>
        protected Task NextAsync(HttpContext context) => _next(context);

        protected async ValueTask WriteResultAsync(
            HttpResponse response,
            IExecutionResult result,
            HttpStatusCode? statusCode,
            CancellationToken cancellationToken)
        {
            response.ContentType = _resultSerializer.GetContentType(result);
            response.StatusCode = (int)(statusCode ?? _resultSerializer.GetStatusCode(result));

            await _resultSerializer.SerializeAsync(result, response.Body, cancellationToken);
        }

        protected async Task<IExecutionResult> ExecuteSingleAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IHttpRequestInterceptor requestInterceptor,
            GraphQLRequest request)
        {
            QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);

            await requestInterceptor.OnCreateAsync(
                context, requestExecutor, requestBuilder, context.RequestAborted);

            return await requestExecutor.ExecuteAsync(
                requestBuilder.Create(), context.RequestAborted);
        }

        protected async Task<IBatchQueryResult> ExecuteOperationBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IHttpRequestInterceptor requestInterceptor,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            var requestBatch = new IReadOnlyQueryRequest[operationNames.Count];

            for (var i = 0; i < operationNames.Count; i++)
            {
                QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);
                requestBuilder.SetOperation(operationNames[i]);

                await requestInterceptor.OnCreateAsync(
                    context, requestExecutor, requestBuilder, context.RequestAborted);

                requestBatch[i] = requestBuilder.Create();
            }

            return await requestExecutor.ExecuteBatchAsync(
                requestBatch, cancellationToken: context.RequestAborted);
        }

        protected async Task<IBatchQueryResult> ExecuteBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IHttpRequestInterceptor requestInterceptor,
            IReadOnlyList<GraphQLRequest> requests)
        {
            var requestBatch = new IReadOnlyQueryRequest[requests.Count];

            for (var i = 0; i < requests.Count; i++)
            {
                QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(requests[0]);

                await requestInterceptor.OnCreateAsync(
                    context, requestExecutor, requestBuilder, context.RequestAborted);

                requestBatch[i] = requestBuilder.Create();
            }

            return await requestExecutor.ExecuteBatchAsync(
                requestBatch, cancellationToken: context.RequestAborted);
        }

        protected static AllowedContentType ParseContentType(string s)
        {
            ReadOnlySpan<char> span = s.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == ';')
                {
                    span = span.Slice(0, i);
                    break;
                }
            }

            if (span.SequenceEqual(ContentType.JsonSpan()))
            {
                return AllowedContentType.Json;
            }

            return AllowedContentType.None;
        }

        /// <summary>
        /// Resolves the executor for the selected schema.
        /// </summary>
        /// <param name="cancellationToken">
        /// The request cancellation token.
        /// </param>
        /// <returns>
        /// Returns the resolved schema.
        /// </returns>
        protected async ValueTask<IRequestExecutor> GetExecutorAsync(
            CancellationToken cancellationToken)
        {
            IRequestExecutor? executor = _executor;

            if (executor is null)
            {
                await _semaphore.WaitAsync(cancellationToken);

                try
                {
                    if (_executor is null)
                    {
                        executor = await _executorResolver.GetRequestExecutorAsync(
                            SchemaName, cancellationToken);
                        _executor = executor;
                    }
                    else
                    {
                        executor = _executor;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return executor;
        }

        private void EvictRequestExecutor(object? sender, RequestExecutorEvictedEventArgs args)
        {
            if (!_disposed && args.Name.Equals(SchemaName))
            {
                _semaphore.Wait();
                try
                {
                    _executor = null;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _executor = null;
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
