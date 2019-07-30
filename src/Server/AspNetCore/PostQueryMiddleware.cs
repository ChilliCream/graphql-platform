using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;
using HotChocolate.Execution.Batching;
using System.Threading;
using System.Linq;
using System.Text;

#if ASPNETCLASSIC
using Microsoft.Owin;
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
    public class PostQueryMiddleware
        : QueryMiddlewareBase
    {
        private const string _batchOperations = "batchOperations";
        private const byte _leftBracket = (byte)'[';
        private const byte _rightBracket = (byte)']';
        private const byte _comma = (byte)',';
        private readonly RequestHelper _requestHelper;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IBatchQueryExecutor _batchExecutor;

#if ASPNETCLASSIC
        public PostQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchExecutor,
            IQueryResultSerializer resultSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            OwinContextAccessor owinContextAccessor,
            QueryMiddlewareOptions options)
            : base(next,
                resultSerializer,
                owinContextAccessor,
                options,
                queryExecutor.Schema.Services)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _batchExecutor = batchExecutor
                ?? throw new ArgumentNullException(nameof(batchExecutor));

            _requestHelper = new RequestHelper(
                documentCache,
                documentHashProvider,
                options.MaxRequestSize,
                options.ParserOptions);
        }
#else
        public PostQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IBatchQueryExecutor batchExecutor,
            IQueryResultSerializer resultSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            QueryMiddlewareOptions options)
            : base(next, resultSerializer, options)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _batchExecutor = batchExecutor
                ?? throw new ArgumentNullException(nameof(batchExecutor));

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
                    await WriteResponseAsync(context.Response, result)
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

            await WriteResponseAsync(context.Response, result)
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

            IResponseStream stream = await _batchExecutor
                .ExecuteAsync(requestBatch, context.GetCancellationToken())
                .ConfigureAwait(false);

            await WriteBatchResultAsync(context, stream)
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

            IResponseStream stream = await _batchExecutor
                .ExecuteAsync(requestBatch, context.GetCancellationToken())
                .ConfigureAwait(false);

            await WriteBatchResultAsync(context, stream)
                .ConfigureAwait(false);
        }

        private async Task WriteBatchResultAsync(
            HttpContext context,
            IResponseStream stream)
        {
            // TODO : we might want different stream result types ... we might want to put that im a serializer that can be injected.
            SetResponseHeaders(context.Response);

            context.Response.Body.WriteByte(_leftBracket);

            await WriteNextResultAsync(context, stream, false)
                .ConfigureAwait(false);

            while (!stream.IsCompleted)
            {
                await WriteNextResultAsync(context, stream, true)
                    .ConfigureAwait(false);
            }

            context.Response.Body.WriteByte(_rightBracket);
        }

        private async Task WriteNextResultAsync(
            HttpContext context,
            IResponseStream stream,
            bool delimiter)
        {
            CancellationToken requestAborted = context.GetCancellationToken();

            IReadOnlyQueryResult result =
                await stream.ReadAsync(requestAborted)
                    .ConfigureAwait(false);

            if (result == null)
            {
                return;
            }

            if (delimiter)
            {
                context.Response.Body.WriteByte(_comma);
            }

            await WriteBatchResponseAsync(context.Response, result)
                .ConfigureAwait(false);

            await context.Response.Body.FlushAsync(
                requestAborted)
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

                switch (context.Request.ContentType.Split(';')[0])
                {
                    case ContentType.Json:
                        batch = await _requestHelper
                            .ReadJsonRequestAsync(stream)
                            .ConfigureAwait(false);
                        break;

                    case ContentType.GraphQL:
                        batch = await _requestHelper
                            .ReadGraphQLQueryAsync(stream)
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException();
                }

                return batch;
            }
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
    }
}
