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
        private readonly RequestHelper _requestHelper;
        private readonly NameString _schemaName;
        private IRequestExecutor? _executor;
        private bool _disposed;

        public HttpPostMiddleware(
            HttpRequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            RequestHelper requestHelper,
            NameString schemaName)
        {
            _next = next;
            _executorResolver = executorResolver;
            _resultSerializer = resultSerializer;
            _requestHelper = requestHelper;
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

            int statusCode = 200;
            IExecutionResult? result;

            try
            {
                IReadOnlyList<GraphQLRequest> requests = await ReadRequestAsync(
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
                        await ExecuteQueryAsync(context, executor, requests[0])
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
                result = QueryResultBuilder.CreateError(ex.Errors);
            }
            catch (GraphQLException ex)
            {
                statusCode = 500;
                result = QueryResultBuilder.CreateError(ex.Errors);
            }
            catch (Exception ex)
            {
                statusCode = 500;
                // TODO : handle exception
            }

            // TODO : serialize result.
        }

        private async Task ExecuteQueryAsync(
            HttpContext context,
            IRequestExecutor executor,
            GraphQLRequest request)
        {
            QueryRequestBuilder builder =
                QueryRequestBuilder.From(request);

            AddContextData(builder, context);

            IExecutionResult result = await executor.ExecuteAsync(
                builder.Create(),
                context.RequestAborted)
                .ConfigureAwait(false);

            Debug.Assert(result is IQueryResult);

            context.Response.ContentType = _resultSerializer.GetContentType(result);
            context.Response.StatusCode = _resultSerializer.GetStatusCode(result);
            
            await _resultSerializer.SerializeAsync(
                result,
                context.Response.Body,
                context.RequestAborted)
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
                    batch = await _requestHelper.ReadJsonRequestAsync(body, cancellationToken)
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
            throw new NotImplementedException();
        }
    }
}
