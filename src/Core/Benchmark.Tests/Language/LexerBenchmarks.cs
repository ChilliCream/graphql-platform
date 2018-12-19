using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser]
    public class LexerBenchmarks
    {
        private readonly ParserBenchmarkResources _resources =
            new ParserBenchmarkResources();
        private readonly Lexer _lexer = new Lexer();

        [Benchmark]
        public SyntaxToken KitchenSinkQuery()
        {
            return _lexer.Read(_resources.KitchenSinkQuery);
        }

        [Benchmark]
        public SyntaxToken IntrospectionQuery()
        {
            return _lexer.Read(_resources.IntrospectionQuery);
        }

        [Benchmark]
        public SyntaxToken SimpleQuery()
        {
            return _lexer.Read(_resources.SimpleQuery);
        }

        [Benchmark]
        public SyntaxToken KitchenSinkSchema()
        {
            return _lexer.Read(_resources.KitchenSinkSchema);
        }

        [Benchmark]
        public SyntaxToken SimpleSchema()
        {
            return _lexer.Read(_resources.SimpleSchema);
        }
    }
}
