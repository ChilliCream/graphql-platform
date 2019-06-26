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

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseProperty(ref request);
            }

            throw new NotImplementedException();
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
                        }
                        break;

                    case _n:
                        if (fieldName.SequenceEqual(_queryName))
                        {
                            request.NamedQuery = _reader.GetString();
                        }
                        break;

                    case _q:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Query = ParseQuery();
                        }
                        break;
                }
            }

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                switch (fieldName[0])
                {

                }
            }
        }

        private IDictionary<string, object> ParseValue()
        {

        }

        private IDictionary<string, object> ParseObject()
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

            var fields = new List<ObjectFieldNode>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                fields.Add(ParseObjectField(isConstant));
            }

            // skip closing token
            Expect(TokenKind.RightBrace);


        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(IDictionary<string, object> obj)
        {
            NameNode name = ParseName();
            ExpectColon();
            IValueNode value = ParseValueLiteral(isConstant);

            Location location = CreateLocation(in start);

            return new ObjectFieldNode
            (
                location,
                name,
                value
            );
        }

        private DocumentNode ParseQuery()
        {
            int length = checked(_reader.Value.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[] unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8Helper.Unescape(
                    _reader.Value,
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

            public DocumentNode Query { get; set; }

            public IDictionary<string, object> Variables { get; set; }

            public IDictionary<string, object> Extensions { get; set; }
        }
    }

    public class GraphQLRequest
    {
        public string OperationName { get; set; }

        public string NamedQuery { get; set; }

        public DocumentNode Query { get; set; }

        public object Variables { get; set; }
    }
}
