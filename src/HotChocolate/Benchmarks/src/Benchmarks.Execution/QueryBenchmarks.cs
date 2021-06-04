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

        public async Task Print_Sessions_TitleAndAbstract() =>
            await PrintQueryPlanAsync(@"
                {
                    sessions(first: 50) {
                        nodes {
                            title
                            abstract
                        }
                    }
                }
            ");

        [Benchmark]
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

        public async Task Print_Sessions_TitleAndAbstractAndTrackName() =>
            await PrintQueryPlanAsync(@"
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
