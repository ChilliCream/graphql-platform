using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser, RankColumn]
    public class ParserBenchmarksV9
    {
        private readonly ParserBenchmarkResourcesV9 _resources =
            new ParserBenchmarkResourcesV9();
        private readonly Parser _parserV8 = new Parser();
        private readonly Utf8Parser _parserV9 = new Utf8Parser();
        private readonly ParserOptions _noLocation = new ParserOptions(noLocations: true);
        private readonly ParserOptions _withLocation = new ParserOptions();
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
        public DocumentNode IntrospectionQueryWithHotChocolateV8NoLocation()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            return _parserV8.Parse(new Source(source), _noLocation);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public DocumentNode IntrospectionQueryWithHotChocolateV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            return _parserV8.Parse(new Source(source), _withLocation);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public DocumentNode IntrospectionQueryWithHotChocolateV9NoLocation()
        {
            var source = new ReadOnlySpan<byte>(_resources.IntrospectionQuery);
            return _parserV9.Parse(source, _noLocation);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-Binary")]
        public DocumentNode IntrospectionQueryWithHotChocolateV9()
        {
            return _parserV9.Parse(_resources.IntrospectionQuery, _withLocation);
        }

        [Benchmark]
        [BenchmarkCategory("KitchenSinkQuery-Binary")]
        public GraphQLParser.AST.GraphQLDocument KitchenSinkQueryWithGraphQLDotNet()
        {
            string source = Encoding.UTF8.GetString(
                _resources.KitchenSinkQuery);
            var sourceObj = new GraphQLParser.Source(source);
            return _parserGD.Parse(sourceObj);
        }

        [Benchmark]
        [BenchmarkCategory("KitchenSinkQuery-Binary")]
        public DocumentNode KitchenSinkQueryQueryWithHotChocolateV8NoLocation()
        {
            string source = Encoding.UTF8.GetString(
                _resources.KitchenSinkQuery);
            return _parserV8.Parse(new Source(source), _noLocation);
        }

        [Benchmark]
        [BenchmarkCategory("KitchenSinkQuery-Binary")]
        public DocumentNode KitchenSinkQueryWithHotChocolateV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.KitchenSinkQuery);
            return _parserV8.Parse(new Source(source), _withLocation);
        }

        [Benchmark]
        [BenchmarkCategory("KitchenSinkQuery-Binary")]
        public DocumentNode KitchenSinkQueryWithHotChocolateV9NoLocation()
        {
            var source = new ReadOnlySpan<byte>(_resources.KitchenSinkQuery);
            return _parserV9.Parse(source, _noLocation);
        }

        [Benchmark]
        [BenchmarkCategory("KitchenSinkQuery-Binary")]
        public DocumentNode KitchenSinkQueryWithHotChocolateV9()
        {
            return _parserV9.Parse(_resources.KitchenSinkQuery, _withLocation);
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
            return _parserV8.Parse(new Source(_introspectionQuery), _noLocation);
        }

        [Benchmark]
        [BenchmarkCategory("Introspection-String")]
        public DocumentNode IntrospectionQueryWithHotChocolateV9FromString()
        {
            var bytes = Encoding.UTF8.GetBytes(_introspectionQuery);
            return _parserV9.Parse(bytes, _noLocation);
        }

    }
}
