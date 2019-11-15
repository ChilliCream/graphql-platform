using System.Globalization;
using System;
using System.Collections.Generic;
using System.Buffers;
using HotChocolate.Language.Properties;
using System.Text;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private const string _persistedQuery = "persistedQuery";
        private readonly IDocumentHashProvider? _hashProvider;
        private readonly IDocumentCache? _cache;
        private readonly bool _useCache;
        private Utf8GraphQLReader _reader;
        private ParserOptions _options;

        public Utf8GraphQLRequestParser(
            ReadOnlySpan<byte> requestData,
            ParserOptions options,
            IDocumentCache cache,
            IDocumentHashProvider hashProvider)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _cache = cache;
            _hashProvider = hashProvider;

            _reader = new Utf8GraphQLReader(requestData);
            _useCache = cache != null;
        }

        public Utf8GraphQLRequestParser(
            ReadOnlySpan<byte> requestData,
            ParserOptions options)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));

            _reader = new Utf8GraphQLReader(requestData);
            _cache = null;
            _hashProvider = null;
            _useCache = false;
        }

        public Utf8GraphQLRequestParser(ReadOnlySpan<byte> requestData)
        {
            _options = ParserOptions.Default;

            _reader = new Utf8GraphQLReader(requestData);
            _cache = null;
            _hashProvider = null;
            _useCache = false;
        }

        public IReadOnlyList<GraphQLRequest> Parse()
        {
            _reader.MoveNext();

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                GraphQLRequest singleRequest = ParseRequest();
                return new[] { singleRequest };
            }

            if (_reader.Kind == TokenKind.LeftBracket)
            {
                return ParseBatchRequest();
            }

            // TODO : resources
            throw new SyntaxException(
                _reader,
                "Expected `{` or `[` as first syntax token.");
        }

        public GraphQLSocketMessage ParseMessage()
        {
            _reader.MoveNext();
            _reader.Expect(TokenKind.LeftBrace);

            var message = new Message();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseMessageProperty(ref message);
            }

            if (message.Type is null)
            {
                throw new InvalidOperationException(
                    "The GraphQL socket message had no type property specified.");
            }

            return new GraphQLSocketMessage
            (
                message.Type,
                message.Id,
                message.Payload,
                message.HasPayload
            );
        }

        public object? ParseJson()
        {
            _reader.MoveNext();
            return ParseValue();
        }

        private IReadOnlyList<GraphQLRequest> ParseBatchRequest()
        {
            var batch = new List<GraphQLRequest>();

            _reader.Expect(TokenKind.LeftBracket);

            while (_reader.Kind != TokenKind.RightBracket)
            {
                batch.Add(ParseRequest());
                _reader.MoveNext();
            }

            return batch;
        }

        private GraphQLRequest ParseRequest()
        {
            var request = new Request();

            _reader.Expect(TokenKind.LeftBrace);

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseRequestProperty(ref request);
            }

            if (!request.HasQuery
                && request.QueryName == null)
            {
                if (_useCache
                    && request.Extensions != null
                    && request.Extensions.TryGetValue(_persistedQuery, out object? obj)
                    && obj is IReadOnlyDictionary<string, object> persistedQuery
                    && persistedQuery.TryGetValue(_hashProvider!.Name, out obj)
                    && obj is string hash)
                {
                    request.QueryName = hash;
                }
                else
                {
                    // TODO : resources
                    throw new SyntaxException(
                        _reader,
                        "Either the query `property` or the `namedQuery` " +
                        "property have to have a value.");
                }
            }

            if (request.HasQuery)
            {
                ParseQuery(ref request);
            }

            return new GraphQLRequest
            (
                request.Document,
                request.QueryName,
                request.QueryHash,
                request.OperationName,
                request.Variables,
                request.Extensions
            );
        }

        private void ParseRequestProperty(ref Request request)
        {
            ReadOnlySpan<byte> fieldName = _reader.Expect(TokenKind.String);
            _reader.Expect(TokenKind.Colon);

            switch (fieldName[0])
            {
                case _o:
                    if (fieldName.SequenceEqual(_operationName))
                    {
                        request.OperationName = ParseStringOrNull();
                        return;
                    }
                    break;

                case _n:
                    if (fieldName.SequenceEqual(_queryName))
                    {
                        if (request.QueryName is null)
                        {
                            request.QueryName = ParseStringOrNull();
                        }
                        else
                        {
                            SkipValue();
                        }
                        return;
                    }
                    break;

                case _i:
                    if (fieldName.SequenceEqual(_id))
                    {
                        if (request.QueryName is null)
                        {
                            request.QueryName = ParseStringOrNull();
                        }
                        else
                        {
                            SkipValue();
                        }
                        return;
                    }
                    break;

                case _q:
                    if (fieldName.SequenceEqual(_query))
                    {
                        request.HasQuery = !IsNullToken();

                        if (request.HasQuery && _reader.Kind != TokenKind.String)
                        {
                            // TODO : resources
                            throw new SyntaxException(
                                _reader,
                                "The query field must be a string or null.");
                        }

                        request.Query = _reader.Value;
                        _reader.MoveNext();
                        return;
                    }
                    break;

                case _v:
                    if (fieldName.SequenceEqual(_variables))
                    {
                        request.Variables = ParseObjectOrNull();
                        return;
                    }
                    break;

                case _e:
                    if (fieldName.SequenceEqual(_extensions))
                    {
                        request.Extensions = ParseObjectOrNull();
                        return;
                    }
                    break;

                default:
                    SkipValue();
                    break;
            }
        }

        private void ParseMessageProperty(ref Message message)
        {
            ReadOnlySpan<byte> fieldName = _reader.Expect(TokenKind.String);
            _reader.Expect(TokenKind.Colon);

            switch (fieldName[0])
            {
                case _t:
                    if (fieldName.SequenceEqual(_type))
                    {
                        message.Type = ParseStringOrNull();
                        return;
                    }
                    break;

                case _i:
                    if (fieldName.SequenceEqual(_id))
                    {
                        message.Id = ParseStringOrNull();
                        return;
                    }
                    break;

                case _p:
                    if (fieldName.SequenceEqual(_payload))
                    {
                        int start = _reader.Start;
                        message.HasPayload = !IsNullToken();
                        int end = SkipValue();
                        message.Payload = _reader.GraphQLData.Slice(
                            start, end - start);
                        return;
                    }
                    break;

                default:
                    // TODO : resources
                    throw new SyntaxException(
                        _reader,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Unexpected request property name `{0}` found.",
                            Utf8GraphQLReader.GetString(fieldName, false)));
            }
        }

        private void ParseQuery(ref Request request)
        {
            int length = checked(request.Query.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[]? unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            DocumentNode? document = null;

            try
            {
                Utf8Helper.Unescape(request.Query, ref unescapedSpan, false);

                if (_useCache)
                {
                    if (request.QueryName is null)
                    {
                        request.QueryName =
                            request.QueryHash =
                            _hashProvider!.ComputeHash(unescapedSpan);
                    }

                    if (!_cache!.TryGetDocument(
                        request.QueryName,
                        out document))
                    {
                        document = Utf8GraphQLParser.Parse(unescapedSpan, _options);

                        if (request.QueryHash is null)
                        {
                            request.QueryHash =
                                _hashProvider!.ComputeHash(unescapedSpan);
                        }
                    }
                }
                else
                {
                    document = Utf8GraphQLParser.Parse(unescapedSpan, _options);
                }

                request.Document = document;
            }
            finally
            {
                if (unescapedArray != null)
                {
                    unescapedSpan.Clear();
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        public static IReadOnlyList<GraphQLRequest> Parse(
           ReadOnlySpan<byte> requestData) =>
           new Utf8GraphQLRequestParser(requestData).Parse();

        public static IReadOnlyList<GraphQLRequest> Parse(
            ReadOnlySpan<byte> requestData,
            ParserOptions options) =>
            new Utf8GraphQLRequestParser(requestData, options).Parse();

        public static IReadOnlyList<GraphQLRequest> Parse(
            string sourceText) =>
            Parse(sourceText, ParserOptions.Default);

        public static unsafe IReadOnlyList<GraphQLRequest> Parse(
            string sourceText,
            ParserOptions options)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            int length = checked(sourceText.Length * 4);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[]? source = null;

            Span<byte> sourceSpan = useStackalloc
                ? stackalloc byte[length]
                : (source = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
                var parser = new Utf8GraphQLRequestParser(sourceSpan, options);
                return parser.Parse();
            }
            finally
            {
                if (source != null)
                {
                    sourceSpan.Clear();
                    ArrayPool<byte>.Shared.Return(source);
                }
            }
        }

        public static GraphQLSocketMessage ParseMessage(
            ReadOnlySpan<byte> messageData) =>
            new Utf8GraphQLRequestParser(messageData).ParseMessage();

        public static object? ParseJson(
            ReadOnlySpan<byte> jsonData) =>
            new Utf8GraphQLRequestParser(jsonData).ParseJson();

        public static object? ParseJson(
            ReadOnlySpan<byte> jsonData,
            ParserOptions options) =>
            new Utf8GraphQLRequestParser(jsonData, options).ParseJson();

        public static object? ParseJson(string sourceText) =>
            ParseJson(sourceText, ParserOptions.Default);

        public static unsafe object? ParseJson(
            string sourceText,
            ParserOptions options)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            int length = checked(sourceText.Length * 4);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[]? source = null;

            Span<byte> sourceSpan = useStackalloc
                ? stackalloc byte[length]
                : (source = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8GraphQLParser.ConvertToBytes(sourceText, ref sourceSpan);
                var parser = new Utf8GraphQLRequestParser(sourceSpan, options);
                return parser.ParseJson();
            }
            finally
            {
                if (source != null)
                {
                    sourceSpan.Clear();
                    ArrayPool<byte>.Shared.Return(source);
                }
            }
        }
    }
}
