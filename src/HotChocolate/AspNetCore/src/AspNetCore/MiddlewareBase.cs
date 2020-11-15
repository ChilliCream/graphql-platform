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
        private readonly RequestDelegate _next;
        private readonly IHttpResultSerializer _resultSerializer;
        private bool _disposed;

        protected MiddlewareBase(
            RequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
        {
            if (executorResolver == null)
            {
                throw new ArgumentNullException(nameof(executorResolver));
            }

            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _resultSerializer = resultSerializer ??
                throw new ArgumentNullException(nameof(executorResolver));
            SchemaName = schemaName;
            ExecutorProxy = new RequestExecutorProxy(executorResolver, schemaName);
        }

        /// <summary>
        /// Gets the name of the schema that this middleware serves up.
        /// </summary>
        protected NameString SchemaName { get; }

        /// <summary>
        /// Gets the request executor proxy.
        /// </summary>
        protected RequestExecutorProxy ExecutorProxy { get; }

        /// <summary>
        /// Invokes the next middleware in line.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/>.
        /// </param>
        protected Task NextAsync(HttpContext context) => _next(context);

        public ValueTask<IRequestExecutor> GetExecutorAsync(
            CancellationToken cancellationToken) =>
            ExecutorProxy.GetRequestExecutorAsync(cancellationToken);

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
            GraphQLRequest request,
            OperationType[]? allowedOperations = null)
        {
            QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);
            requestBuilder.SetAllowedOperations(allowedOperations);

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                ExecutorProxy.Dispose();
                _disposed = true;
            }
        }
    }
}
