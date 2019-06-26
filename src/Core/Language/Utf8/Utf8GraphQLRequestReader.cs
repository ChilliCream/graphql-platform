using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestReader
    {
        private const byte _o = (byte)'o';
        private const byte _n = (byte)'n';
        private const byte _q = (byte)'q';
        private const byte _v = (byte)'v';
        private const byte _e = (byte)'e';

        private static readonly byte[] _operationName = new[]
        {
            (byte)'o',
            (byte)'p',
            (byte)'e',
            (byte)'r',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'N',
            (byte)'a',
            (byte)'m',
            (byte)'e'
        };

        private static readonly byte[] _queryName = new[]
        {
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
            (byte)'d',
            (byte)'Q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static readonly byte[] _query = new[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static readonly byte[] _variables = new[]
        {
            (byte)'v',
            (byte)'a',
            (byte)'r',
            (byte)'i',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e',
            (byte)'s'
        };

        private static readonly byte[] _extension = new[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'s',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        private readonly IDocumentHashProvider _hashProvider;
        private readonly IDocumentCache _cache;
        private Utf8GraphQLReader _reader;
        private ParserOptions _options;


        public Utf8GraphQLRequestReader(
            ReadOnlySpan<byte> requestData,
            ParserOptions options)
        {
            _reader = new Utf8GraphQLReader(requestData);
            _options = options;
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
            throw new NotImplementedException();
        }

        private GraphQLRequest ParseRequest()
        {
            var request = new Request();

            _reader.Expect(TokenKind.LeftBrace);


            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseProperty(ref request);
                _reader.MoveNext();
            }

            if (request.Query.Length == 0)
            {
                throw new SyntaxException(_reader, "RESOURCES");
            }

            if (request.NamedQuery is null)
            {
                request.NamedQuery =
                    _hashProvider.ComputeHash(request.Query);
            }

            if(!_cache.TryGet(request.NamedQuery, out DocumentNode document))
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

            if (_reader.Kind == TokenKind.String)
            {
                switch (fieldName[0])
                {
                    case _o:
                        if (fieldName.SequenceEqual(_operationName))
                        {
                            request.OperationName = _reader.GetString();
                            return;
                        }
                        break;

                    case _n:
                        if (fieldName.SequenceEqual(_queryName))
                        {
                            request.NamedQuery = _reader.GetString();
                            return;
                        }
                        break;

                    case _q:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Query = _reader.Value;
                            return;
                        }
                        break;
                }
                throw new SyntaxException(_reader, "RESOURCES");
            }

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                switch (fieldName[0])
                {
                    case _v:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Variables = ParseObject();
                            return;
                        }
                        break;

                    case _e:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Extensions = ParseObject();
                            return;
                        }
                        break;
                }
            }

            throw new SyntaxException(_reader, "RESOURCES");
        }

        private object ParseValue()
        {
            switch (_reader.Kind)
            {
                case TokenKind.LeftBracket:
                    return ParseList();

                case TokenKind.LeftBrace:
                    return ParseObject();

                case TokenKind.Name:
                case TokenKind.String:
                case TokenKind.Integer:
                case TokenKind.Float:
                    return ParseScalarValue();

                default:
                    throw new SyntaxException(_reader, "RESOURCES");
            }
        }

        private IReadOnlyDictionary<string, object> ParseObject()
        {
            if (_reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in _reader)));
            }

            _reader.Expect(TokenKind.LeftBrace);

            var obj = new Dictionary<string, object>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseObjectField(obj);
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return obj;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(IDictionary<string, object> obj)
        {
            if (_reader.Kind != TokenKind.String)
            {
                throw new SyntaxException(_reader, "RESOURCES");
            }

            string name = _reader.GetString();
            _reader.Expect(TokenKind.Colon);
            object value = ParseValue();
            obj.Add(name, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyList<object> ParseList()
        {
            if (_reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var list = new List<object>();

            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                list.Add(ParseValue());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ParseScalarValue()
        {
            switch (_reader.Kind)
            {
                case TokenKind.String:
                    return _reader.GetString();

                case TokenKind.Integer:
                    return long.Parse(_reader.GetScalarValue());

                case TokenKind.Float:
                    return decimal.Parse(_reader.GetScalarValue());

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        return true;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        return false;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        return null;
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

        private ref struct Request
        {
            public string OperationName { get; set; }

            public string NamedQuery { get; set; }

            public ReadOnlySpan<byte> Query { get; set; }

            public IReadOnlyDictionary<string, object> Variables { get; set; }

            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }
    }

    public class GraphQLRequest
    {
        public GraphQLRequest(
            string operationName,
            string namedQuery,
            DocumentNode query,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> extensions)
        {
            if (string.IsNullOrEmpty(namedQuery))
            {
                throw new ArgumentException("message", nameof(namedQuery));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }
        }

        public string OperationName { get; set; }

        public string NamedQuery { get; set; }

        public DocumentNode Query { get; set; }

        public object Variables { get; set; }
    }

    public interface IDocumentCache
    {
        bool TryGet(string key, out DocumentNode document);
    }

    public interface IDocumentHashProvider
    {
        string ComputeHash(ReadOnlySpan<byte> document);
    }
}
