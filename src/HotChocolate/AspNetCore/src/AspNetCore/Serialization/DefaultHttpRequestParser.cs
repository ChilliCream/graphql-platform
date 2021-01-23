using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Language.Utf8GraphQLRequestParser;
using static HotChocolate.AspNetCore.ThrowHelper;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace HotChocolate.AspNetCore.Serialization
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
            _documentCache = documentCache ??
                throw new ArgumentNullException(nameof(documentCache));
            _documentHashProvider = documentHashProvider ??
                throw new ArgumentNullException(nameof(documentHashProvider));
            _maxRequestSize = maxRequestSize < _minRequestSize
                ? _minRequestSize
                : maxRequestSize;
            _parserOptions = parserOptions ??
                throw new ArgumentNullException(nameof(parserOptions));
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
            IReadOnlyDictionary<string, object?>? extensions = null;

            // if we have no query or query id we cannot execute anything.
            if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(queryId))
            {
                // so, if we do not find a top-level query or top-level id we will try to parse
                // the extensions and look in the extensions for Apollo`s active persisted
                // query extensions.
                if ((string)parameters[_extensionsIdentifier] is { Length: > 0 } se)
                {
                    extensions = ParseJsonObject(se);
                }

                // we will use the request parser utils to extract the has from the extensions.
                if (!TryExtractHash(extensions, _documentHashProvider, out var hash))
                {
                    // if we cannot find any query hash in the extensions or if the extensions are
                    // null we are unable to execute and will throw a request error.
                    throw DefaultHttpRequestParser_QueryAndIdMissing();
                }

                // if we however found a query hash we will use it as a query id and move on
                // to execute the query.
                queryId = hash;
            }

            try
            {
                DocumentNode? document = string.IsNullOrEmpty(query) 
                    ? null 
                    : Utf8GraphQLParser.Parse(query);
                    
                IReadOnlyDictionary<string, object?>? variables = null;

                // if we find variables we do need to parse them
                if ((string)parameters[_variablesIdentifier] is { Length: > 0 } sv)
                {
                    variables = ParseVariables(sv);
                }

                if (extensions is null &&
                    (string)parameters[_extensionsIdentifier] is { Length: > 0 } se)
                {
                    extensions = ParseJsonObject(se);
                }

                return new GraphQLRequest(
                    document,
                    queryId,
                    null,
                    operationName,
                    variables,
                    extensions);
            }
            catch (SyntaxException ex)
            {
                throw DefaultHttpRequestParser_SyntaxError(ex);
            }
            catch (Exception ex)
            {
                throw DefaultHttpRequestParser_UnexpectedError(ex);
            }
        }

        // todo: The IFormCollection is convenient, but it requires us to work with strings instead of a byte stream
        //       and in order to use the Utf8GraphQLRequestParser we have to convert those strings back to bytes - yikes!
        public IReadOnlyList<GraphQLRequest> ReadFormRequest(IFormCollection form)
        {
            if (form.Count < 2)
                throw new Exception("At least a 'operations' and 'map' field need to be present.");

            if (!form.TryGetValue("operations", out StringValues operationsCollection) || !operationsCollection.TryPeek(out var operationsString))
                throw new Exception("No 'operations' specified.");

            if (!form.TryGetValue("map", out StringValues mapCollection) || !mapCollection.TryPeek(out var mapString))
                throw new Exception("No 'map' specified.");

            var map = JsonSerializer.Deserialize<IDictionary<string, string[]>>(mapString);

            if (map == null)
                throw new Exception("'map' is not a dictionary of string[].");

            var operationsBytes = Encoding.UTF8.GetBytes(operationsString);

            var requestParser = new Utf8GraphQLRequestParser(
                operationsBytes,
                _parserOptions,
                _documentCache,
                _documentHashProvider);

            var requests = requestParser.Parse();

            if (requests == null)
                throw new Exception("Requests could not be parsed.");

            foreach (var request in requests)
            {
                // todo: This is quite hacky and requires Utf8GraphQLRequestParser to actually return this
                //       I choose to do it this way as to not change the existing APIs.
                //       For a final implementation this should be done differently
                if (!(request.Variables is Dictionary<string, object?> mutableVariables))
                    continue;

                foreach (var (key, values) in map)
                {
                    var file = form.Files.GetFile(key);

                    if (file == null)
                        throw new Exception($"File could not be parsed for key '{key}'");

                    foreach (var value in values)
                    {
                        var parts = value?.Split(".");

                        if (parts == null || parts.Length < 2)
                            throw new Exception("Failed to parse 'map' value.");

                        var variableName = parts[1];

                        if (parts.Length == 2)
                        {
                            // single file upload, e.g. 'variables.file'
                            mutableVariables[variableName] = file;
                        }
                        else
                        {
                            // multiple files upload, e.g. 'variables.files.1'
                            if (mutableVariables[variableName] is List<IFormFile> list)
                            {
                                list.Add(file);
                            }
                            else
                            {
                                mutableVariables[variableName] = new List<IFormFile>
                                {
                                    file
                                };
                            }
                        }
                    }
                }
            }

            return requests;
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
                            throw DefaultHttpRequestParser_RequestIsEmpty();
                        }

                        return isGraphQLQuery
                            ? ParseQuery(buffer, bytesBuffered)
                            : ParseRequest(buffer, bytesBuffered);
                    },
                    bytesBuffered =>
                    {
                        if (bytesBuffered > _maxRequestSize)
                        {
                            throw DefaultHttpRequestParser_MaxRequestSizeExceeded();
                        }
                    },
                    cancellationToken);
            }
            catch (SyntaxException ex)
            {
                throw DefaultHttpRequestParser_SyntaxError(ex);
            }
            catch (Exception ex)
            {
                throw DefaultHttpRequestParser_UnexpectedError(ex);
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

            var requestParser = new Utf8GraphQLParser(graphQLData, _parserOptions);

            string queryHash = _documentHashProvider.ComputeHash(graphQLData);
            DocumentNode document = requestParser.Parse();

            return new[] { new GraphQLRequest(document, queryHash) };
        }
    }
}
