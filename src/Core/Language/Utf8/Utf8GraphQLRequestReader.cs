using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestReader
    {
        private const byte _o = (byte)'o';
        private const byte _o = (byte)'n';
        private const byte _o = (byte)'q';
        private const byte _o = (byte)'v';
        private const byte _o = (byte)'e';

        private Utf8GraphQLReader _reader;


        public Utf8GraphQLRequestReader(
            ReadOnlySpan<byte> requestData,
            ParserOptions options)
        {
            _reader = new Utf8GraphQLReader(requestData);
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
            _reader.MoveNext();

            if (_reader.Kind == TokenKind.String)
            {
                switch (_reader.Value[0])
                {

                }
            }
        }



        private ref struct Request
        {
            public string OperationName { get; set; }

            public string NamedQuery { get; set; }

            public DocumentNode Query { get; set; }

            public object Variables { get; set; }
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
