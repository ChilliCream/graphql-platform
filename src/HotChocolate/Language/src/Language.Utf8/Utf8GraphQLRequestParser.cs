using System;
using System.Buffers;
using System.Collections.Generic;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private const string _persistedQuery = "persistedQuery";
        private readonly IDocumentHashProvider? _hashProvider;
        private readonly IDocumentCache? _cache;
        private readonly bool _useCache;
        private readonly ParserOptions _options;
        private Utf8GraphQLReader _reader;

        public Utf8GraphQLRequestParser(
            ReadOnlySpan<byte> requestData,
            ParserOptions? options = null,
            IDocumentCache? cache = null,
            IDocumentHashProvider? hashProvider = null)
        {
            _reader = new Utf8GraphQLReader(requestData);
            _options = options ?? ParserOptions.Default;
            _cache = cache;
            _hashProvider = hashProvider;
            _useCache = cache is not null;
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

            throw ThrowHelper.InvalidRequestStructure(_reader);
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
                message.Payload
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

            if (!request.HasQuery && request.QueryId == null)
            {
                if (_useCache
                    && request.Extensions != null
                    && request.Extensions.TryGetValue(_persistedQuery, out object? obj)
                    && obj is IReadOnlyDictionary<string, object> persistedQuery
                    && persistedQuery.TryGetValue(_hashProvider!.Name, out obj)
                    && obj is string hash)
                {
                    request.QueryId = hash;
                }
                else
                {
                    throw ThrowHelper.NoIdAndNoQuery(_reader);
                }
            }

            if (request.HasQuery)
            {
                ParseQuery(ref request);
            }

            if (request.Document is null && request.QueryId is null)
            {
                throw ThrowHelper.NoIdAndNoQuery(_reader);
            }

            return new GraphQLRequest
            (
                request.Document,
                request.QueryId,
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
                    if (fieldName.SequenceEqual(OperationName))
                    {
                        request.OperationName = ParseStringOrNull();
                    }
                    break;

                case _i:
                    if (fieldName.SequenceEqual(Id))
                    {
                        request.QueryId = ParseStringOrNull();
                    }
                    break;

                case _q:
                    if (fieldName.SequenceEqual(Query))
                    {
                        request.HasQuery = !IsNullToken();

                        if (request.HasQuery && _reader.Kind != TokenKind.String)
                        {
                            throw ThrowHelper.QueryMustBeStringOrNull(_reader);
                        }

                        request.Query = _reader.Value;
                        _reader.MoveNext();
                    }
                    break;

                case _v:
                    if (fieldName.SequenceEqual(Variables))
                    {
                        request.Variables = ParseVariables();
                    }
                    break;

                case _e:
                    if (fieldName.SequenceEqual(Extensions))
                    {
                        request.Extensions = ParseObjectOrNull();
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
                    if (fieldName.SequenceEqual(Type))
                    {
                        message.Type = ParseStringOrNull();
                    }
                    break;

                case _i:
                    if (fieldName.SequenceEqual(Id))
                    {
                        message.Id = ParseStringOrNull();
                    }
                    break;

                case _p:
                    if (fieldName.SequenceEqual(Payload))
                    {
                        var start = _reader.Start;
                        var hasPayload = !IsNullToken();
                        var end = SkipValue();
                        message.Payload = hasPayload
                            ? _reader.GraphQLData.Slice(start, end - start)
                            : default;
                    }
                    break;

                default:
                    throw ThrowHelper.UnexpectedProperty(_reader, fieldName);
            }
        }

        private void ParseQuery(ref Request request)
        {
            var length = request.Query.Length;

            byte[]? unescapedArray = null;

            Span<byte> unescapedSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8Helper.Unescape(request.Query, ref unescapedSpan, false);
                var queryId = request.QueryId;
                DocumentNode? document;

                if (_useCache)
                {
                    queryId ??= request.QueryHash = _hashProvider!.ComputeHash(unescapedSpan);

                    if (!_cache!.TryGetDocument(queryId, out document))
                    {
                        document = unescapedSpan.Length == 0
                            ? null
                            : Utf8GraphQLParser.Parse(unescapedSpan, _options);

                        request.QueryHash ??= _hashProvider!.ComputeHash(unescapedSpan);
                    }
                }
                else
                {
                    document = Utf8GraphQLParser.Parse(unescapedSpan, _options);
                }

                if (document is not null)
                {
                    request.Document = document;
                    if (queryId is not null && request.QueryId is null)
                    {
                        request.QueryId = queryId;
                    }
                }
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
            ReadOnlySpan<byte> requestData,
            ParserOptions? options = null)
        {
            options ??= ParserOptions.Default;
            return new Utf8GraphQLRequestParser(requestData, options).Parse();
        }

        public static unsafe IReadOnlyList<GraphQLRequest> Parse(
            string sourceText,
            ParserOptions? options = null)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    LangResources.SourceText_Empty,
                    nameof(sourceText));
            }

            options ??= ParserOptions.Default;

            var length = checked(sourceText.Length * 4);
            byte[]? source = null;

            Span<byte> sourceSpan = length <= GraphQLConstants.StackallocThreshold
                ? stackalloc byte[length]
                : source = ArrayPool<byte>.Shared.Rent(length);

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
    }
}
