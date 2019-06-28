using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using System.Buffers;
using HotChocolate.Language;

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
        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;

        public PostQueryMiddleware(
            RequestDelegate next,
            IQueryExecutor queryExecutor,
            IQueryResultSerializer resultSerializer,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            QueryMiddlewareOptions options)
                : base(next, queryExecutor, resultSerializer, options)
        {
            _documentCache = documentCache;
            _documentHashProvider = documentHashProvider
                ?? new MD5DocumentHashProvider();
        }

        protected override bool CanHandleRequest(HttpContext context)
        {
            return string.Equals(
                context.Request.Method,
                HttpMethods.Post,
                StringComparison.Ordinal);
        }

        protected override async Task<IQueryRequestBuilder>
            CreateQueryRequestAsync(HttpContext context)
        {
            bool isGraphQLQuery = false;

            switch (context.Request.ContentType.Split(';')[0])
            {
                case ContentType.Json:
                    isGraphQLQuery = false;
                    break;

                case ContentType.GraphQL:
                    isGraphQLQuery = true;
                    break;

                default:
                    throw new NotSupportedException();
            }

            using (Stream stream = context.Request.Body)
            {
                // TODO : batching support has to be added later
                IReadOnlyList<GraphQLRequest> batch =
                    await ReadRequestAsync(stream, isGraphQLQuery)
                        .ConfigureAwait(false);

                GraphQLRequest request = batch[0];

                return QueryRequestBuilder.New()
                    .SetQuery(request.Query)
                    .SetQueryName(request.QueryName)
                    .SetQueryName(request.QueryName) // TODO : we should have a hash here
                    .SetOperation(request.OperationName)
                    .SetVariableValues(request.Variables)
                    .SetProperties(request.Extensions);
            }
        }

        private async Task<IReadOnlyList<GraphQLRequest>> ReadRequestAsync(
            Stream stream, bool isGraphQLQuery)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            var bytesBuffered = 0;

            try
            {
                while (true)
                {
                    var bytesRemaining = buffer.Length - bytesBuffered;

                    if (bytesRemaining == 0)
                    {
                        var next = ArrayPool<byte>.Shared.Rent(
                            buffer.Length * 2);
                        Buffer.BlockCopy(buffer, 0, next, 0, buffer.Length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = next;
                        bytesRemaining = buffer.Length - bytesBuffered;
                    }

                    var bytesRead = await stream.ReadAsync(
                        buffer, bytesBuffered, bytesRemaining)
                        .ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    bytesBuffered += bytesRead;
                    if (bytesBuffered > Options.MaxRequestSize)
                    {
                        throw new QueryException(
                            ErrorBuilder.New()
                                .SetMessage("Max request size reached.")
                                .SetCode("MAX_REQUEST_SIZE")
                                .Build());
                    }
                }

                return isGraphQLQuery
                    ? ParseQuery(buffer, bytesBuffered)
                    : ParseRequest(buffer, bytesBuffered);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private IReadOnlyList<GraphQLRequest> ParseRequest(
            byte[] buffer, int bytesBuffered)
        {
            var graphQLData = new ReadOnlySpan<byte>(buffer);
            graphQLData = graphQLData.Slice(0, bytesBuffered);

            var requestParser = new Utf8GraphQLRequestParser(
                graphQLData,
                Options.ParserOptions,
                _documentCache,
                _documentHashProvider);

            return requestParser.Parse();
        }

        private IReadOnlyList<GraphQLRequest> ParseQuery(
            byte[] buffer, int bytesBuffered)
        {
            var graphQLData = new ReadOnlySpan<byte>(buffer);
            graphQLData = graphQLData.Slice(0, bytesBuffered);

            var requestParser = new Utf8GraphQLParser(
                graphQLData, Options.ParserOptions);

            string queryHash = _documentHashProvider.ComputeHash(graphQLData);
            DocumentNode document = requestParser.Parse();

            return new[] { new GraphQLRequest(document, queryHash) };
        }
    }
}
