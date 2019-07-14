using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Buffers;
using HotChocolate.Execution;
using HotChocolate.Language;

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
                ? maxRequestSize
                : maxRequestSize;
            _parserOptions = parserOptions
                ?? throw new ArgumentNullException(nameof(parserOptions));
        }

        public Task<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
            Stream stream) => ReadAsync(stream, false);

        public Task<IReadOnlyList<GraphQLRequest>> ReadGraphQLQueryAsync(
            Stream stream) => ReadAsync(stream, true);

        private async Task<IReadOnlyList<GraphQLRequest>> ReadAsync(
            Stream stream,
            bool isGraphQLQuery)
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
                    if (bytesBuffered > _maxRequestSize)
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
