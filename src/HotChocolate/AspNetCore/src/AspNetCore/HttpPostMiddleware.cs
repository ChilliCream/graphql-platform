using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Claims;
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
        private readonly IRequestParser _requestParser;
        private readonly NameString _schemaName;
        private IRequestExecutor? _executor;
        private bool _disposed;

        public HttpPostMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            IRequestParser requestParser,
            NameString schemaName)
        {
            _next = next;
            _executorResolver = executorResolver;
            _resultSerializer = resultSerializer;
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
                await _next(context).ConfigureAwait(false);
            }
            else
            {
                await HandleRequestAsync(context, contentType, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }

        public async Task HandleRequestAsync(
            HttpContext context,
            AllowedContentType contentType,
            CancellationToken cancellationToken)
        {
            // first we need to gather an executor to start execution the request.
            IRequestExecutor executor = await GetExecutorAsync(context.RequestAborted)
                .ConfigureAwait(false);
            IErrorHandler errorHandler = executor.Services.GetRequiredService<IErrorHandler>();

            IExecutionResult? result = null;
            int? statusCode = null;

            try
            {
                IReadOnlyList<GraphQLRequest>? requests = await ReadRequestAsync(
                    contentType, context.Request.Body, context.RequestAborted)
                    .ConfigureAwait(false);

                if (requests.Count == 0)
                {
                    // IError error = ErrorHandler.Handle(ErrorHelper.RequestHasNoElements());
                    // httpHelper.Result = CreateError(error);
                }
                else if (requests.Count == 1)
                {
                    string operations = context.Request.Query[_batchOperations];

                    if (operations is null)
                    {
                        result = await ExecuteQueryAsync(context, executor, requests[0])
                            .ConfigureAwait(false);
                    }
                    else if (TryParseOperations(operations, out IReadOnlyList<string>? operationNames))
                    {
                        // await ExecuteOperationBatchAsync(
                        //     httpHelper, batch[0], operationNames)
                        //    .ConfigureAwait(false);
                    }
                    else
                    {
                        // IError error = ErrorHandler.Handle(ErrorHelper.InvalidRequest());
                        // httpHelper.StatusCode = BadRequest;
                        // httpHelper.Result = CreateError(error);
                    }
                }
                else
                {

                }
            }
            catch (GraphQLRequestException ex)
            {
                statusCode = 400;
                IEnumerable<IError> errors = errorHandler.Handle(ex.Errors);
                result = QueryResultBuilder.CreateError(errors);
            }
            catch (Exception ex)
            {
                statusCode = 500;
                IError error = errorHandler.CreateUnexpectedError(ex).Build();
                result = QueryResultBuilder.CreateError(error);
            }

            Debug.Assert(result is { });

            await WriteResultAsync(context.Response, result, statusCode, context.RequestAborted)
                .ConfigureAwait(false);
        }

        private Task<IExecutionResult> ExecuteQueryAsync(
            HttpContext context,
            IRequestExecutor executor,
            GraphQLRequest request)
        {
            QueryRequestBuilder builder =
                QueryRequestBuilder.From(request);

            AddContextData(builder, context);

            return executor.ExecuteAsync(builder.Create(), context.RequestAborted);
        }

        private async ValueTask WriteResultAsync(
            HttpResponse response,
            IExecutionResult result,
            int? statusCode,
            CancellationToken cancellationToken)
        {
            response.ContentType = _resultSerializer.GetContentType(result);
            response.StatusCode = statusCode ?? _resultSerializer.GetStatusCode(result);

            await _resultSerializer.SerializeAsync(
                result,
                response.Body,
                cancellationToken)
                .ConfigureAwait(false);
        }

        private IQueryRequestBuilder AddContextData(
            IQueryRequestBuilder builder,
            HttpContext context)
        {
            builder.TrySetServices(context.RequestServices);
            builder.TryAddProperty(nameof(ClaimsPrincipal), context.User);
            builder.TryAddProperty(nameof(CancellationToken), context.RequestAborted);

            if (context.IsTracingEnabled())
            {
                builder.TryAddProperty(ContextDataKeys.EnableTracing, true);
            }

            return builder;
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
            IReadOnlyList<GraphQLRequest>? batch = null;

            switch (contentType)
            {
                case AllowedContentType.Json:
                    batch = await _requestParser.ReadJsonRequestAsync(body, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException();
            }

            return batch;
        }

        private static AllowedContentType ParseContentType(string s)
        {
            var span = s.AsSpan();

            for (int i = 0; i < span.Length; i++)
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
