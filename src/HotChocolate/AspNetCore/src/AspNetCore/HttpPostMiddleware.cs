using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.AspNetCore.Utilities;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;
using static HotChocolate.Execution.QueryResultBuilder;

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
            _requestHelper = requestHelper;
            _schemaName = schemaName;
            executorResolver.RequestExecutorEvicted += EvictRequestExecutor;
        }



        public async Task InvokeAsync(HttpContext context)
        {
            AllowedContentType contentType = ParseContentType(context.Request.ContentType);

            if (contentType == AllowedContentType.None)
            {

            }

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
                        // await ExecuteQueryAsync(httpHelper, batch[0]).ConfigureAwait(false);
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

    public interface IHttpResultSerializer
    {
        string GetContentType(IExecutionResult result);


    }

    public enum AllowedContentType
    {
        None,
        Json
    }


    public class HttpPostMiddleware2
        : QueryMiddlewareBase
    {
        private const string _batchOperations = "batchOperations";
        private readonly RequestHelper _requestHelper;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IBatchQueryExecutor _batchExecutor;
        private readonly IQueryResultSerializer _resultSerializer;
        private readonly IResponseStreamSerializer _streamSerializer;

        public HttpPostMiddleware2(
            RequestDelegate next,
            IHttpPostMiddlewareOptions options,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchQueryExecutor,
            IQueryResultSerializer resultSerializer,
            IResponseStreamSerializer streamSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            IErrorHandler errorHandler)
            : base(next, options, resultSerializer, errorHandler)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _batchExecutor = batchQueryExecutor
                ?? throw new ArgumentNullException(nameof(batchQueryExecutor));
            _resultSerializer = resultSerializer
                ?? throw new ArgumentNullException(nameof(resultSerializer));
            _streamSerializer = streamSerializer
                ?? throw new ArgumentNullException(nameof(streamSerializer));

            _requestHelper = new RequestHelper(
                documentCache,
                documentHashProvider,
                options.MaxRequestSize,
                options.ParserOptions);
        }

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Post,
                StringComparison.Ordinal);
        }

        protected override async Task ExecuteRequestAsync(HttpHelper httpHelper)
        {
            IReadOnlyList<GraphQLRequest> batch =
                await ReadRequestAsync(httpHelper.Context)
                    .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                IError error = ErrorHandler.Handle(ErrorHelper.RequestHasNoElements());
                httpHelper.Result = CreateError(error);
            }
            else if (batch.Count == 1)
            {
                string operations = httpHelper.Context.Request.Query[_batchOperations];

                if (operations == null)
                {
                    await ExecuteQueryAsync(httpHelper, batch[0]).ConfigureAwait(false);
                }
                else if (TryParseOperations(operations, out IReadOnlyList<string>? operationNames))
                {
                    await ExecuteOperationBatchAsync(
                        httpHelper, batch[0], operationNames)
                        .ConfigureAwait(false);
                }
                else
                {
                    IError error = ErrorHandler.Handle(ErrorHelper.InvalidRequest());
                    httpHelper.StatusCode = BadRequest;
                    httpHelper.Result = CreateError(error);
                }
            }
            else
            {
                await ExecuteQueryBatchAsync(httpHelper, batch).ConfigureAwait(false);
            }
        }

        private async Task ExecuteQueryAsync(HttpHelper httpHelper, GraphQLRequest request)
        {
            IReadOnlyQueryRequest queryRequest =
                await BuildRequestAsync(
                    httpHelper.Context,
                    httpHelper.Services,
                    QueryRequestBuilder.From(request))
                    .ConfigureAwait(false);

            httpHelper.StatusCode = OK;
            httpHelper.Result = await _queryExecutor
                .ExecuteAsync(queryRequest, httpHelper.Context.RequestAborted)
                .ConfigureAwait(false);
        }


        private async Task ExecuteOperationBatchAsync(
            HttpHelper httpHelper,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            IReadOnlyList<IReadOnlyQueryRequest> requestBatch =
                await BuildBatchRequestAsync(
                    httpHelper.Context, httpHelper.Services, request, operationNames)
                    .ConfigureAwait(false);

            httpHelper.StatusCode = OK;
            httpHelper.StreamSerializer = _streamSerializer;
            httpHelper.Result = await _batchExecutor
                .ExecuteAsync(requestBatch, httpHelper.Context.RequestAborted)
                .ConfigureAwait(false);
        }

        private async Task ExecuteQueryBatchAsync(
            HttpHelper httpHelper,
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

        private async Task<IReadOnlyList<IReadOnlyQueryRequest>> BuildBatchRequestAsync(
            HttpContext context,
            IServiceProvider services,
            IReadOnlyList<GraphQLRequest> batch)
        {
            var queryBatch = new IReadOnlyQueryRequest[batch.Count];

            for (var i = 0; i < batch.Count; i++)
            {
                queryBatch[i] = await BuildRequestAsync(
                    context,
                    services,
                    QueryRequestBuilder.From(batch[i]))
                    .ConfigureAwait(false);
            }

            return queryBatch;
        }

        private async Task<IReadOnlyList<IReadOnlyQueryRequest>> BuildBatchRequestAsync(
            HttpContext context,
            IServiceProvider services,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            var queryBatch = new IReadOnlyQueryRequest[operationNames.Count];

            for (var i = 0; i < operationNames.Count; i++)
            {
                IQueryRequestBuilder requestBuilder =
                    QueryRequestBuilder.From(request)
                        .SetOperation(operationNames[i]);

                queryBatch[i] = await BuildRequestAsync(
                    context,
                    services,
                    requestBuilder)
                    .ConfigureAwait(false);
            }

            return queryBatch;
        }

        protected async Task<IReadOnlyList<GraphQLRequest>> ReadRequestAsync(
            HttpContext context)
        {
            Stream stream = context.Request.Body;
            IReadOnlyList<GraphQLRequest>? batch = null;

            switch (ParseContentType(context.Request.ContentType))
            {
                case AllowedContentType.Json:
                    batch = await _requestHelper
                        .ReadJsonRequestAsync(
                            stream,
                            context.GetCancellationToken())
                        .ConfigureAwait(false);
                    break;

                case AllowedContentType.GraphQL:
                    batch = await _requestHelper
                        .ReadGraphQLQueryAsync(
                            stream,
                            context.GetCancellationToken())
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException();

            }

            return batch;
        }

        private static AllowedContentType ParseContentType(string s)
        {
            ReadOnlySpan<char> span = s.AsSpan();

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == ';')
                {
                    span = span.Slice(0, i);
                    break;
                }
            }

            if (span.SequenceEqual(ContentType.GraphQLSpan()))
            {
                return AllowedContentType.GraphQL;
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
    }

    internal static class ErrorHelper
    {
        public static IError InvalidRequest() =>
            ErrorBuilder.New()
                .SetMessage("Invalid GraphQL Request.")
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();

        public static IError RequestHasNoElements() =>
            ErrorBuilder.New()
                .SetMessage("The GraphQL batch request has no elements.")
                .SetCode(ErrorCodes.Server.RequestInvalid)
                .Build();
    }
}
