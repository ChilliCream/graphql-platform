using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser]
    public class ParserBenchmarksV9
    {
        private readonly ParserBenchmarkResourcesV9 _resources =
            new ParserBenchmarkResourcesV9();
        private readonly Parser _parserV8 = new Parser();
        private readonly Utf8Parser _parserV9 = new Utf8Parser();
        private readonly ParserOptions _options = new ParserOptions(noLocations: true);
        private readonly GraphQLParser.Parser _parserGD =
            new GraphQLParser.Parser(new GraphQLParser.Lexer());
        private readonly string _introspectionQuery;

        public ParserBenchmarksV9()
        {
            _introspectionQuery = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public GraphQLParser.AST.GraphQLDocument IntrospectionWithGraphQLDotNet()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            var sourceObj = new GraphQLParser.Source(source);
            return _parserGD.Parse(sourceObj);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public DocumentNode IntrospectionQueryWithHotChocolateV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            return _parserV8.Parse(new Source(source), _options);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public DocumentNode IntrospectionQueryWithHotChocolateV9()
        {
            var source = new ReadOnlySpan<byte>(_resources.IntrospectionQuery);
            return _parserV9.Parse(source, _options);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public GraphQLParser.AST.GraphQLDocument IntrospectionQueryWithGraphQLDotNetFromString()
        {
            var sourceObj = new GraphQLParser.Source(_introspectionQuery);
            return _parserGD.Parse(sourceObj);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public DocumentNode IntrospectionQueryWithHotChocolateV8FromString()
        {
            return _parserV8.Parse(new Source(_introspectionQuery), _options);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public DocumentNode IntrospectionQueryWithHotChocolateV9FromString()
        {
            var bytes = Encoding.UTF8.GetBytes(_introspectionQuery);
            var source = new ReadOnlySpan<byte>(bytes);
            return _parserV9.Parse(source, _options);
        }

    }
}
