using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.ConferencePlanner;
using HotChocolate.Execution;

namespace HotChocolate.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class IntrospectionBenchmarks : BenchmarkBase
    {
        [Benchmark]
        public async Task Query_TypeName() =>
            await BenchmarkAsync(@"
                {
                    __typename
                }
            ");
        public async Task Print_Query_TypeName() =>
            await PrintQueryPlanAsync(@"
                {
                    __typename
                }
            ");

        [Benchmark]
        public async Task Query_Introspection() =>
            await BenchmarkAsync(Introspection);
        public string Introspection { get; } = Resources.Introspection;
        public async Task Print_Query_Introspection() =>
            await PrintQueryPlanAsync(Introspection);


        [Benchmark]
        public async Task Query_Introspection_Prepared() =>
            await BenchmarkAsync(IntrospectionRequest);
        public IQueryRequest IntrospectionRequest { get; } = Prepare(Resources.Introspection);
    }
}