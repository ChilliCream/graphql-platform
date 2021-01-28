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
        private const string _operations = "operations";
        private const string _map = "map";

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

        // TODO : this method should be split up
        public IReadOnlyList<GraphQLRequest> ReadFormRequest(
            IFormCollection form)
        {
            string? operations = null;
            IDictionary<string, string[]>? map = null;

            foreach (KeyValuePair<string, StringValues> field in form)
            {
                switch (field.Key)
                {
                    case _operations:
                        if (!field.Value.TryPeek(out operations) || string.IsNullOrEmpty(operations))
                        {
                            // TODO : throw helper
                            throw new GraphQLRequestException(
                                ErrorBuilder.New()
                                    .SetMessage("No '{0}' specified.", _operations)
                                    .SetCode("// TODO CODE HC")
                                    .Build());
                        }
                        break;
                    case _map:
                        if (string.IsNullOrEmpty(operations))
                        {
                            // TODO : throw helper
                            throw new GraphQLRequestException(
                                ErrorBuilder.New()
                                    .SetMessage("Misordered multipart fields; '{0}' should follow ‘{1}’.", _map, _operations)
                                    .SetCode("// TODO CODE HC")
                                    .Build());
                        }

                        if (!field.Value.TryPeek(out var mapString))
                        {
                            // TODO : throw helper
                            throw new GraphQLRequestException(
                                ErrorBuilder.New()
                                    .SetMessage("No '{0}' specified.", _map)
                                    .SetCode("// TODO CODE HC")
                                    .Build());
                        }

                        try
                        {
                            map = JsonSerializer.Deserialize<IDictionary<string, string[]>>(mapString);
                        }
                        catch
                        {
                            // TODO : throw helper
                            throw new GraphQLRequestException(
                                ErrorBuilder.New()
                                    .SetMessage(
                                        "Invalid JSON in the ‘{0}’ multipart field.", _map)
                                    .SetCode("// TODO CODE HC")
                                    .Build());
                        }
                        break;
                    default:
                        // TODO : throw helper
                        throw new GraphQLRequestException(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "At least an '{0}' and a '{1}' field need to be present.",
                                    _operations,
                                    _map)
                                .SetCode("// TODO CODE HC")
                                .Build());
                }
            }

            IReadOnlyList<GraphQLRequest> requests =
                Parse(operations, _parserOptions, _documentCache, _documentHashProvider);

            // No files were sent, we can exit early.
            if (form.Files.Count < 1)
            {
                return requests;
            }

            if (map is null)
            {
                // TODO : throw helper
                throw new GraphQLRequestException(
                    ErrorBuilder.New()
                        .SetMessage("Misordered multipart fields; files should follow ‘{0}’.", _map)
                        .SetCode("// TODO CODE HC")
                        .Build());
            }

            foreach (GraphQLRequest request in requests)
            {
                if (!(request.Variables is Dictionary<string, object?> mutableVariables))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string[]> mapPair in map)
                {
                    var filename = mapPair.Key;
                    var objectPaths = mapPair.Value;

                    if (string.IsNullOrEmpty(filename))
                    {
                        // TODO : how to handle
                        continue;
                    }

                    if (objectPaths is null || objectPaths.Length < 1)
                    {
                        // TODO : how to handle
                        continue;
                    }

                    IFormFile? file = form.Files.GetFile(filename);

                    if (file is null)
                    {
                        // TODO : how to handle
                        continue;
                    }

                    foreach (var objectPath in objectPaths)
                    {
                        var parts = objectPath.Split('.');

                        if (parts.Length < 2)
                        {
                            // TODO : how to handle
                            continue;
                        }


                        if (parts[0] != "variables")
                        {
                            // nested properties are currently not supported
                            continue;
                        }

                        var variableName = parts[1];

                        switch (parts.Length)
                        {
                            case 2:
                                // single file upload, e.g. 'variables.file'
                                mutableVariables[variableName] = file;
                                break;
                            case 3:
                                // multi file upload, e.g. 'variables.files.1'
                                if (!int.TryParse(parts[2], out var fileIndex))
                                {
                                    continue;
                                }

                                List<IFormFile?> list;

                                if (mutableVariables[variableName] is List<IFormFile?> variableList)
                                {
                                    list = variableList;
                                }
                                else
                                {
                                    list = new List<IFormFile?>();

                                    mutableVariables[variableName] = list;
                                }

                                // we don't know the size of the file list beforehand so we have to resize dynamically
                                for(var i = list.Count; i <= fileIndex; i++)
                                {
                                    list.Add(null);
                                }

                                list[fileIndex] = file;

                                break;
                            default:
                                // nested object, which is currently not supported
                                break;
                        }
                    }
                }
            }

            return requests;
        }

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
