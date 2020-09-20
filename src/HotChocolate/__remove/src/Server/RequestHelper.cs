using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Server
{
    public class RequestHelper
    {
        private const int _minRequestSize = 256;
        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;
        private readonly ParserOptions _parserOptions;
        private readonly int _maxRequestSize;

        public RequestHelper(
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider,
            int maxRequestSize,
            ParserOptions parserOptions)
        {
            _documentCache = documentCache
                ?? throw new ArgumentNullException(nameof(documentCache));
            _documentHashProvider = documentHashProvider
                ?? throw new ArgumentNullException(nameof(documentHashProvider));
            _maxRequestSize = maxRequestSize < _minRequestSize
                ? _minRequestSize
                : maxRequestSize;
            _parserOptions = parserOptions
                ?? throw new ArgumentNullException(nameof(parserOptions));
        }

        public Task<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
            Stream stream,
            CancellationToken cancellationToken) =>
            ReadAsync(stream, false, cancellationToken);

        public Task<IReadOnlyList<GraphQLRequest>> ReadGraphQLQueryAsync(
            Stream stream,
            CancellationToken cancellationToken) =>
            ReadAsync(stream, true, cancellationToken);

        private Task<IReadOnlyList<GraphQLRequest>> ReadAsync(
            Stream stream,
            bool isGraphQLQuery,
            CancellationToken cancellationToken)
        {
            return BufferHelper.ReadAsync(
                stream,
                (buffer, bytesBuffered) =>
                {
                    if (bytesBuffered == 0)
                    {
                        // TODO : resources
                        throw new QueryException(
                            ErrorBuilder.New()
                                .SetMessage("The GraphQL request is empty.")
                                .SetCode(ErrorCodes.Server.RequestInvalid)
                                .Build());
                    }

                    return isGraphQLQuery
                        ? ParseQuery(buffer, bytesBuffered)
                        : ParseRequest(buffer, bytesBuffered);
                },
                bytesBuffered =>
                {
                    if (bytesBuffered > _maxRequestSize)
                    {
                        // TODO : resources
                        throw new QueryException(
                            ErrorBuilder.New()
                                .SetMessage("Max GraphQL request size reached.")
                                .SetCode(ErrorCodes.Server.MaxRequestSize)
                                .Build());
                    }
                },
                cancellationToken);
        }

        private IReadOnlyList<GraphQLRequest> ParseRequest(
            byte[] buffer, int bytesBuffered)
        {
            var graphQLData = new ReadOnlySpan<byte>(buffer);
            graphQLData = graphQLData.Slice(0, bytesBuffered);

            var requestParser = new Utf8GraphQLRequestParser(
                graphQLData,
                _parserOptions,
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
                graphQLData, _parserOptions);

            string queryHash = _documentHashProvider.ComputeHash(graphQLData);
            DocumentNode document = requestParser.Parse();

            return new[] { new GraphQLRequest(document, queryHash) };
        }
    }
}
