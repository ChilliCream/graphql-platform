using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser, RankColumn]
    public class LexerBenchmarksV9
    {
        private readonly ParserBenchmarkResourcesV9 _resources =
            new ParserBenchmarkResourcesV9();
        private readonly Lexer _lexer = new Lexer();
        private readonly GraphQLParser.Lexer _lexerGD =
            new GraphQLParser.Lexer();
        private readonly string _introspectionQuery;

        public LexerBenchmarksV9()
        {
            _introspectionQuery = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public GraphQLParser.Token IntrospectionWithGraphQLDotNet()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            var sourceObj = new GraphQLParser.Source(source);
            GraphQLParser.Token token = _lexerGD.Lex(sourceObj);

            while (token.Kind != GraphQLParser.TokenKind.EOF)
            {
                token = _lexerGD.Lex(sourceObj, token.End);
            }

            return token;
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public SyntaxToken IntrospectionQueryWithHotChocolateV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            return _lexer.Read(new Source(source));
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public void IntrospectionQueryWithHotChocolateV9()
        {
            var source = new ReadOnlySpan<byte>(_resources.IntrospectionQuery);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public GraphQLParser.Token IntrospectionQueryWithGraphQLDotNetFromString()
        {
            var sourceObj = new GraphQLParser.Source(_introspectionQuery);
            GraphQLParser.Token token = _lexerGD.Lex(sourceObj);

            while (token.Kind != GraphQLParser.TokenKind.EOF)
            {
                token = _lexerGD.Lex(sourceObj, token.End);
            }

            return token;
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public SyntaxToken IntrospectionQueryWithHotChocolateV8FromString()
        {
            return _lexer.Read(new Source(_introspectionQuery));
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public void IntrospectionQueryWithHotChocolateV9FromString()
        {
            var bytes = Encoding.UTF8.GetBytes(_introspectionQuery);
            var source = new ReadOnlySpan<byte>(bytes);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }
    }
}
