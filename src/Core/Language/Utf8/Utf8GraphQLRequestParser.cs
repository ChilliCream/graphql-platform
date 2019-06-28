using System;
using System.Collections.Generic;
using System.Buffers;

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
            _cache = cache
                ?? throw new ArgumentNullException(nameof(cache));
            _hashProvider = hashProvider
                ?? throw new ArgumentNullException(nameof(hashProvider));

            _reader = new Utf8GraphQLReader(requestData);
            _useCache = true;
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
            throw new SyntaxException(_reader, "Unexpected request structure.");
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
                ParseProperty(ref request);
            }

            if (request.Query.Length == 0)
            {
                throw new SyntaxException(_reader, "RESOURCES");
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
                request.OperationName,
                request.NamedQuery,
                document,
                request.Variables,
                request.Extensions
            );
        }

        private void ParseProperty(ref Request request)
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
                    // TODO : must not be null
                    if (fieldName.SequenceEqual(_query))
                    {
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

            throw new SyntaxException(_reader, "RESOURCES");
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
                Utf8Helper.Unescape(
                    request.Query,
                    ref unescapedSpan,
                    false);

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
    }
}
