using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Utilities
{
    internal class DefaultHttpRequestParser : IHttpRequestParser
    {
        private const int _minRequestSize = 256;
        private const string _queryIdIdentifier = "id";
        private const string _operationNameIdentifier = "operationName";
        private const string _queryIdentifier = "query";
        private const string _variablesIdentifier = "variables";
        private const string _extensionsIdentifier = "extensions";

        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;
        private readonly ParserOptions _parserOptions;
        private readonly int _maxRequestSize;

        public DefaultHttpRequestParser(
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

        public ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
            Stream stream,
            CancellationToken cancellationToken) =>
            ReadAsync(stream, false, cancellationToken);

        public GraphQLRequest ReadParamsRequest(IQueryCollection parameters)
        {
            // next we deserialize the GET request with the query request builder ...
            string query = parameters[_queryIdentifier];
            string queryId = parameters[_queryIdIdentifier];
            string operationName = parameters[_operationNameIdentifier];

            if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(queryId))
            {
                throw new GraphQLRequestException(
                    "Either the parameter {0} or {1} has to be set.");
            }

            try
            {
                DocumentNode document = Utf8GraphQLParser.Parse(query);
                IReadOnlyDictionary<string, object?>? variables = null;
                IReadOnlyDictionary<string, object?>? extensions = null;

                // if we find variables we do need to parse them
                if ((string)parameters[_variablesIdentifier] is { Length: > 0 } sv &&
                    ParseJson(sv) is IReadOnlyDictionary<string, object?> var)
                {
                    variables = var;
                }

                if ((string)parameters[_extensionsIdentifier] is { Length: > 0 } se &&
                    ParseJson(se) is IReadOnlyDictionary<string, object?> ext)
                {
                    extensions = ext;
                }

                return new GraphQLRequest(
                    document, queryId, null, operationName, 
                    variables, extensions);
            }
            catch (SyntaxException ex)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(ex.Message);
            }
        }

        private async ValueTask<IReadOnlyList<GraphQLRequest>> ReadAsync(
            Stream stream,
            bool isGraphQLQuery,
            CancellationToken cancellationToken)
        {
            try
            {
                return await BufferHelper.ReadAsync(
                    stream,
                    (buffer, bytesBuffered) =>
                    {
                        if (bytesBuffered == 0)
                        {
                            // TODO : resources
                            throw new GraphQLRequestException(
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
                            throw new GraphQLRequestException(
                                ErrorBuilder.New()
                                    .SetMessage("Max GraphQL request size reached.")
                                    .SetCode(ErrorCodes.Server.MaxRequestSize)
                                    .Build());
                        }
                    },
                    cancellationToken);
            }
            catch (SyntaxException ex)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(ex.Message);
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
