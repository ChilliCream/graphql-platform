using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.ConferencePlanner;
using HotChocolate.Execution;

namespace HotChocolate.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class QueryBenchmarks : BenchmarkBase
    {
        [Benchmark]
        public async Task Sessions_TitleAndAbstract() =>
            await BenchmarkAsync(@"
                {
                    sessions(first: 50) {
                        nodes {
                            title
                            abstract
                        }
                    }
                }
            ");

        // TODO : this query causes dead-locks ... probably DataLoader
        // [Benchmark]
        public async Task Sessions_TitleAndAbstractAndTrackName() =>
            await BenchmarkAsync(@"
                {
                    sessions {
                        nodes {
                            title
                            abstract
                            track {
                                name
                            }
                        }
                    }
                }
            ");
    }
}
