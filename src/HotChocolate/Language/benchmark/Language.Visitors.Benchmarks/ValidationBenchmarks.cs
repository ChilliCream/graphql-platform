using BenchmarkDotNet.Attributes;
using HotChocolate.Language.Visitors.Benchmarks.Resources;
using HotChocolate.Language.Visitors;
using System.Buffers;
using System;

namespace HotChocolate.Language.Visitors.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class WalkerBenchmarks
    {
        private readonly BenchmarkSyntaxWalker_V1 _walker_v1 = new BenchmarkSyntaxWalker_V1();
        private readonly BenchmarkSyntaxWalker_V2 _walker_v2 = new BenchmarkSyntaxWalker_V2();
        private readonly BenchmarkSyntaxWalker_V3 _walker_v3 = new BenchmarkSyntaxWalker_V3();
        private readonly DocumentNode _introspectionQuery;
        private readonly ArgumentNode _fo = new ArgumentNode("abc", "abc");

        public WalkerBenchmarks()
        {
            var resources = new ResourceHelper();
            _introspectionQuery = Utf8GraphQLParser.Parse(
                resources.GetResourceString("IntrospectionQuery.graphql"));
        }

        [Benchmark]
        public void Walker_V1()
        {
            _walker_v1.Visit(_introspectionQuery, null);
        }

        [Benchmark]
        public void Walker_V2()
        {
            _introspectionQuery.Accept(_walker_v2);
        }


        [Benchmark]
        public void Walker_V3()
        {
            _walker_v3.Visit(_introspectionQuery);
        }
    }
}
