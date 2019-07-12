using System.Globalization;
using System;
using System.Collections.Generic;
using System.Buffers;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private readonly IDocumentHashProvider _hashProvider;
        private readonly IDocumentCache _cache;
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

            return new GraphQLSocketMessage
            (
                message.Type,
                message.Id,
                message.Payload,
                message.HasPayload
            );
        }

        public object ParseJson()
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
                ParseRequest();
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

            if (request.IsQueryNull
                && request.NamedQuery == null)
            {
                // TODO : resources
                throw new SyntaxException(
                    _reader,
                    "Either the query `property` or the `namedQuery` " +
                    "property have to have a value.");
            }

            DocumentNode document;

            if (_useCache)
            {
                if (request.NamedQuery is null)
                {
                    request.NamedQuery =
                        _hashProvider.ComputeHash(request.Query);
                }

                if (!_cache.TryGetDocument(request.NamedQuery, out document))
                {
                    document = ParseQuery(in request);
                }
            }
            else
            {
                document = ParseQuery(in request);
            }

            return new GraphQLRequest
            (
                document,
                request.NamedQuery,
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
                        request.NamedQuery = ParseStringOrNull();
                        return;
                    }
                    break;

                case _q:
                    if (fieldName.SequenceEqual(_query))
                    {
                        request.IsQueryNull = IsNullToken();
                        if (!request.IsQueryNull
                            && _reader.Kind != TokenKind.String)
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
            }

            // TODO : resources
            throw new SyntaxException(
                _reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected request property name `{0}` found.",
                    Utf8GraphQLReader.GetString(fieldName, false)));
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
            }

            // TODO : resources
            throw new SyntaxException(
                _reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unexpected request property name `{0}` found.",
                    Utf8GraphQLReader.GetString(fieldName, false)));
        }

        private DocumentNode ParseQuery(in Request request)
        {
            int length = checked(request.Query.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[] unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8Helper.Unescape(request.Query, ref unescapedSpan, false);
                return Utf8GraphQLParser.Parse(unescapedSpan, _options);
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

            byte[] source = null;

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

        public static object ParseJson(
            ReadOnlySpan<byte> jsonData) =>
            new Utf8GraphQLRequestParser(jsonData).ParseJson();

        public static object ParseJson(
            ReadOnlySpan<byte> jsonData,
            ParserOptions options) =>
            new Utf8GraphQLRequestParser(jsonData, options).ParseJson();

        public static object ParseJson(string sourceText) =>
            ParseJson(sourceText, ParserOptions.Default);

        public static unsafe object ParseJson(
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

            byte[] source = null;

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
