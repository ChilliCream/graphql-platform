using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Language
{
    [CoreJob]
    [RPlotExporter, MemoryDiagnoser]
    public class ParserBenchmarks
    {
        private readonly ParserBenchmarkResources _resources =
            new ParserBenchmarkResources();
        private readonly Parser _parser = new Parser();

        [Benchmark]
        public DocumentNode KitchenSinkQuery()
        {
            return _parser.Parse(_resources.KitchenSinkQuery);
        }

        [Benchmark]
        public DocumentNode IntrospectionQuery()
        {
            return _parser.Parse(_resources.IntrospectionQuery);
        }

        [Benchmark]
        public DocumentNode SimpleQuery()
        {
            return _parser.Parse(_resources.SimpleQuery);
        }

        [Benchmark]
        public DocumentNode KitchenSinkSchema()
        {
            return _parser.Parse(_resources.KitchenSinkSchema);
        }

        [Benchmark]
        public DocumentNode SimpleSchema()
        {
            return _parser.Parse(_resources.SimpleSchema);
        }
    }
}
