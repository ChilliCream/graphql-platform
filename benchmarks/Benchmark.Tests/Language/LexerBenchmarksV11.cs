using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser, RankColumn]
    public class LexerBenchmarksV11
    {
        private readonly ParserBenchmarkResourcesV11 _resources =
            new ParserBenchmarkResourcesV11();
        private readonly Lexer _lexer = new Lexer();
        private readonly string _introspectionQuery;

        public LexerBenchmarksV11()
        {
            _introspectionQuery = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
        }

        [Benchmark]
        public SyntaxToken IntrospectionQueryV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.IntrospectionQuery);
            return _lexer.Read(new Source(source));
        }

        [Benchmark]
        public void IntrospectionQueryV11()
        {
            var source = new ReadOnlySpan<byte>(_resources.IntrospectionQuery);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }

        [Benchmark]
        public SyntaxToken IntrospectionQueryV8FromString()
        {
            return _lexer.Read(new Source(_introspectionQuery));
        }

        [Benchmark]
        public void IntrospectionQueryV11FromString()
        {
            var bytes = Encoding.UTF8.GetBytes(_introspectionQuery);
            var source = new ReadOnlySpan<byte>(bytes);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }

        [Benchmark]
        public SyntaxToken SimpleQueryV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.SimpleQuery);
            return _lexer.Read(new Source(source));
        }

        [Benchmark]
        public void SimpleQueryV11()
        {
            var source = new ReadOnlySpan<byte>(_resources.SimpleQuery);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }

        [Benchmark]
        public SyntaxToken SimpleSchemaV8()
        {
            string source = Encoding.UTF8.GetString(
                _resources.SimpleSchema);
            return _lexer.Read(new Source(source));
        }

        [Benchmark]
        public void SimpleSchemaV11()
        {
            var source = new ReadOnlySpan<byte>(_resources.SimpleSchema);
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }
    }
}
