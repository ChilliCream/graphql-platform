using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Language;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class HttpPostMiddleware : IDisposable
    {
        private const string _batchOperations = "batchOperations";
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly HttpRequestDelegate _next;
        private readonly IRequestExecutorResolver _executorResolver;
        private readonly IHttpResultSerializer _resultSerializer;
        private readonly IHttpRequestInterceptor _requestInterceptor;
        private readonly IRequestParser _requestParser;
        private readonly NameString _schemaName;
        private IRequestExecutor? _executor;
        private bool _disposed;

        public HttpPostMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IHttpRequestInterceptor requestInterceptor,
            IRequestParser requestParser,
            NameString schemaName)
        {
            _next = next;
            _executorResolver = executorResolver;
            _resultSerializer = resultSerializer;
            _requestInterceptor = requestInterceptor;
            _requestParser = requestParser;
            _schemaName = schemaName;
            executorResolver.RequestExecutorEvicted += EvictRequestExecutor;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            AllowedContentType contentType = ParseContentType(context.Request.ContentType);

            if (contentType == AllowedContentType.None)
            {
                // the content type is unknown so we will invoke the next middleware.
                await _next(context);
            }
            else
            {
                await HandleRequestAsync(context, contentType);
            }
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            AllowedContentType contentType)
        {
            // first we need to get the request executor to be able to execute requests.
            IRequestExecutor requestExecutor = await GetExecutorAsync(context.RequestAborted);
            IErrorHandler errorHandler = requestExecutor.Services.GetRequiredService<IErrorHandler>();

            HttpStatusCode? statusCode = null;
            IExecutionResult? result = null;

            try
            {
                // next we parse the GraphQL request.
                IReadOnlyList<GraphQLRequest> requests = await ReadRequestAsync(
                    contentType, context.Request.Body, context.RequestAborted);

                if (requests.Count == 0)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    IError error = errorHandler.Handle(ErrorHelper.RequestHasNoElements());
                    result = QueryResultBuilder.CreateError(error);
                }
                else if (requests.Count == 1)
                {
                    string operationNames = context.Request.Query[_batchOperations];

                    if (operationNames is null)
                    {
                        result = await ExecuteSingleAsync(
                            context, requestExecutor, requests[0]);
                    }
                    else if (TryParseOperations(operationNames, out IReadOnlyList<string>? ops))
                    {
                        result = await ExecuteOperationBatchAsync(
                            context, requestExecutor, requests[0], ops);
                    }
                    else
                    {
                        IError error = errorHandler.Handle(ErrorHelper.InvalidRequest());
                        statusCode = HttpStatusCode.BadRequest;
                        result = QueryResultBuilder.CreateError(error);
                    }
                }
                else
                {

                }
            }
            catch (GraphQLRequestException ex)
            {
                statusCode = HttpStatusCode.BadRequest;
                IEnumerable<IError> errors = errorHandler.Handle(ex.Errors);
                result = QueryResultBuilder.CreateError(errors);
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                IError error = errorHandler.CreateUnexpectedError(ex).Build();
                result = QueryResultBuilder.CreateError(error);
            }

            Debug.Assert(result is not null, "No GraphQL result was created.");
            await WriteResultAsync(context.Response, result, statusCode, context.RequestAborted);
        }

        private async ValueTask WriteResultAsync(
            HttpResponse response,
            IExecutionResult result,
            HttpStatusCode? statusCode,
            CancellationToken cancellationToken)
        {
            response.ContentType = _resultSerializer.GetContentType(result);
            response.StatusCode = (int)(statusCode ?? _resultSerializer.GetStatusCode(result));

            await _resultSerializer.SerializeAsync(result, response.Body, cancellationToken);
        }

        private async Task<IExecutionResult> ExecuteSingleAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            GraphQLRequest request)
        {
            QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);

            await _requestInterceptor.OnCreateAsync(
                context, requestExecutor, requestBuilder, context.RequestAborted);

            return await requestExecutor.ExecuteAsync(
                requestBuilder.Create(), context.RequestAborted);
        }

        private async Task<IBatchQueryResult> ExecuteOperationBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            var requestBatch = new IReadOnlyQueryRequest[operationNames.Count];

            for (var i = 0; i < operationNames.Count; i++)
            {
                QueryRequestBuilder requestBuilder = QueryRequestBuilder.From(request);
                requestBuilder.SetOperation(operationNames[i]);

                await _requestInterceptor.OnCreateAsync(
                    context, requestExecutor, requestBuilder, context.RequestAborted);

                requestBatch[i] = requestBuilder.Create();
            }

            return await requestExecutor.ExecuteBatchAsync(
                requestBatch, cancellationToken: context.RequestAborted);
        }

        private async Task<IBatchQueryResult> ExecuteBatchAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            IReadOnlyList<GraphQLRequest> batch)
        {
            IReadOnlyList<IReadOnlyQueryRequest> requestBatch =
                await BuildBatchRequestAsync(
                        httpHelper.Context,
                        httpHelper.Services,
                        batch)
                    .ConfigureAwait(false);

            httpHelper.StatusCode = OK;
            httpHelper.StreamSerializer = _streamSerializer;
            httpHelper.Result = await _batchExecutor
                .ExecuteAsync(requestBatch, httpHelper.Context.RequestAborted)
                .ConfigureAwait(false);
        }

        private async ValueTask<IRequestExecutor> GetExecutorAsync(
            CancellationToken cancellationToken)
        {
            IRequestExecutor? executor = _executor;

            if (executor is null)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (_executor is null)
                    {
                        executor = await _executorResolver.GetRequestExecutorAsync(
                            _schemaName, cancellationToken)
                            .ConfigureAwait(false);
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

        private async Task<IReadOnlyList<GraphQLRequest>> ReadRequestAsync(
            AllowedContentType contentType,
            Stream body,
            CancellationToken cancellationToken)
        {
            if (contentType == AllowedContentType.Json)
            {
                return await _requestParser.ReadJsonRequestAsync(body, cancellationToken);
            }
            throw new NotSupportedException();
        }

        private static AllowedContentType ParseContentType(string s)
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

        private static bool TryParseOperations(
            string operationNameString,
            [NotNullWhen(true)] out IReadOnlyList<string>? operationNames)
        {
            var reader = new Utf8GraphQLReader(Encoding.UTF8.GetBytes(operationNameString));
            reader.Read();

            if (reader.Kind != TokenKind.LeftBracket)
            {
                operationNames = null;
                return false;
            }

            var names = new List<string>();

            while (reader.Read() && reader.Kind == TokenKind.Name)
            {
                names.Add(reader.GetName());
            }

            if (reader.Kind != TokenKind.RightBracket)
            {
                operationNames = null;
                return false;
            }

            operationNames = names;
            return true;
        }

        private void EvictRequestExecutor(object? sender, RequestExecutorEvictedEventArgs args)
        {
            if (!_disposed && args.Name.Equals(_schemaName))
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
            if (!_disposed)
            {
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
