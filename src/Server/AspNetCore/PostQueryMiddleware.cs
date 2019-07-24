using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;
using HotChocolate.Execution.Batching;
using System.Security.Claims;

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
        private const byte _leftBracket = (byte)'[';
        private const byte _rightBracket = (byte)']';
        private const byte _comma = (byte)',';
        private readonly RequestHelper _requestHelper;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IBatchQueryExecutor _batchExecutor;

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
                IReadOnlyQueryRequest request =
                    await BuildRequestAsync(
                        context,
                        services,
                        QueryRequestBuilder.From(batch[0]))
                        .ConfigureAwait(false);

                IExecutionResult result = await _queryExecutor
                    .ExecuteAsync(request, context.GetCancellationToken())
                    .ConfigureAwait(false);

                await WriteResponseAsync(context.Response, result)
                    .ConfigureAwait(false);
            }
            else
            {
                IReadOnlyList<IReadOnlyQueryRequest> request =
                    await BuildBatchRequestAsync(context, services, batch)
                        .ConfigureAwait(false);

                IResponseStream stream = await _batchExecutor
                    .ExecuteAsync(request, context.GetCancellationToken())
                    .ConfigureAwait(false);

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
        }

        private async Task WriteNextResultAsync(
            HttpContext context,
            IResponseStream stream,
            bool delimiter)
        {
            IReadOnlyQueryResult result =
                await stream.ReadAsync(context.RequestAborted)
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
                context.RequestAborted)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<IReadOnlyQueryRequest>>
            BuildBatchRequestAsync(
                HttpContext context,
                IServiceProvider services,
                IReadOnlyList<GraphQLRequest> batch)
        {
            var queryBatch = new IReadOnlyQueryRequest[batch.Count];

            for (int i = 0; i < batch.Count; i++)
            {
                queryBatch[i] = await BuildRequestAsync(
                    context,
                    services,
                    QueryRequestBuilder.From(batch[i]))
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
    }
}
