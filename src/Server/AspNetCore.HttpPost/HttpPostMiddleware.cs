using System.Threading;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;
using HotChocolate.Execution.Batching;
using System.Text;

#if ASPNETCLASSIC
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class HttpPostMiddleware
        : QueryMiddlewareBase
    {
        private const string _batchOperations = "batchOperations";
        private readonly RequestHelper _requestHelper;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IBatchQueryExecutor _batchExecutor;
        private readonly IQueryResultSerializer _resultSerializer;
        private readonly IResponseStreamSerializer _streamSerializer;

#if ASPNETCLASSIC
        public HttpPostMiddleware(
            RequestDelegate next,
            IHttpPostMiddlewareOptions options,
            OwinContextAccessor owinContextAccessor,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchQueryExecutor,
            IQueryResultSerializer resultSerializer,
            IResponseStreamSerializer streamSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider)
            : base(next,
                options,
                owinContextAccessor,
                queryExecutor.Schema.Services)
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
#else
        public HttpPostMiddleware(
            RequestDelegate next,
            IHttpPostMiddlewareOptions options,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchQueryExecutor,
            IQueryResultSerializer resultSerializer,
            IResponseStreamSerializer streamSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider)
            : base(next, options)
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
#endif

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Post,
                StringComparison.Ordinal);
        }

        protected override async Task ExecuteRequestAsync(
            HttpContext context,
            IServiceProvider services)
        {
            IReadOnlyList<GraphQLRequest> batch =
                await ReadRequestAsync(context)
                    .ConfigureAwait(false);

            if (batch.Count == 1)
            {
                string operations = context.Request.Query[_batchOperations];

                if (operations == null)
                {
                    await ExecuteQueryAsync(context, services, batch[0])
                        .ConfigureAwait(false);
                }
                else if (TryParseOperations(operations,
                    out IReadOnlyList<string> operationNames))
                {
                    await ExecuteOperationBatchAsync(
                        context, services, batch[0], operationNames)
                        .ConfigureAwait(false);
                }
                else
                {
                    // TODO : resources
                    var result = QueryResult.CreateError(
                        ErrorBuilder.New()
                            .SetMessage("Invalid GraphQL Request.")
                            .SetCode("INVALID_REQUEST")
                            .Build());

                    SetResponseHeaders(
                        context.Response,
                        _resultSerializer.ContentType);

                    await _resultSerializer.SerializeAsync(
                        result, context.Response.Body)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await ExecuteQueryBatchAsync(context, services, batch)
                    .ConfigureAwait(false);
            }
        }

        private async Task ExecuteQueryAsync(
            HttpContext context,
            IServiceProvider services,
            GraphQLRequest request)
        {
            IReadOnlyQueryRequest queryRequest =
                await BuildRequestAsync(
                    context,
                    services,
                    QueryRequestBuilder.From(request))
                    .ConfigureAwait(false);

            IExecutionResult result = await _queryExecutor
                .ExecuteAsync(queryRequest, context.GetCancellationToken())
                .ConfigureAwait(false);

            SetResponseHeaders(
                context.Response,
                _resultSerializer.ContentType);

            await _resultSerializer.SerializeAsync(
                result,
                context.Response.Body,
                context.GetCancellationToken())
                .ConfigureAwait(false);
        }


        private async Task ExecuteOperationBatchAsync(
            HttpContext context,
            IServiceProvider services,
            GraphQLRequest request,
            IReadOnlyList<string> operationNames)
        {
            IReadOnlyList<IReadOnlyQueryRequest> requestBatch =
                await BuildBatchRequestAsync(
                    context, services, request, operationNames)
                    .ConfigureAwait(false);

            IResponseStream responseStream = await _batchExecutor
                .ExecuteAsync(requestBatch, context.GetCancellationToken())
                .ConfigureAwait(false);

            SetResponseHeaders(
                context.Response,
                _streamSerializer.ContentType);

            await _streamSerializer.SerializeAsync(
                responseStream,
                context.Response.Body,
                context.GetCancellationToken())
                .ConfigureAwait(false);
        }

        private async Task ExecuteQueryBatchAsync(
            HttpContext context,
            IServiceProvider services,
            IReadOnlyList<GraphQLRequest> batch)
        {
            IReadOnlyList<IReadOnlyQueryRequest> requestBatch =
                await BuildBatchRequestAsync(context, services, batch)
                    .ConfigureAwait(false);

            IResponseStream responseStream = await _batchExecutor
                .ExecuteAsync(requestBatch, context.GetCancellationToken())
                .ConfigureAwait(false);

            SetResponseHeaders(
                context.Response,
                _streamSerializer.ContentType);

            await _streamSerializer.SerializeAsync(
                responseStream,
                context.Response.Body,
                context.GetCancellationToken())
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<IReadOnlyQueryRequest>>
            BuildBatchRequestAsync(
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

        private async Task<IReadOnlyList<IReadOnlyQueryRequest>>
            BuildBatchRequestAsync(
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
            using (Stream stream = context.Request.Body)
            {
                IReadOnlyList<GraphQLRequest> batch = null;

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
            out IReadOnlyList<string> operationNames)
        {
            var reader = new Utf8GraphQLReader(
                Encoding.UTF8.GetBytes(operationNameString));
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

        private enum AllowedContentType
        {
            None,
            GraphQL,
            Json
        }
    }
}
