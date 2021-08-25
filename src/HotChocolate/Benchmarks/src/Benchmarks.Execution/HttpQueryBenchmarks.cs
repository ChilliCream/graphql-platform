using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.ConferencePlanner;

namespace HotChocolate.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class HttpQueryBenchmarks : BenchmarkBase
    {
        [Benchmark]
        public async Task Sessions_TitleAndAbstract_10_Items() =>
            await BenchmarkServerAsync(@"
                {
                    sessions(first: 10) {
                        nodes {
                            title
                            abstract
                        }
                    }
                }
            ");

        [Benchmark]
        public async Task Sessions_TitleAndAbstractAndTrackName_10_Items() =>
            await BenchmarkServerAsync(@"
                {
                    sessions(first: 10) {
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

        [Benchmark]
        public async Task Sessions_Medium() =>
            await BenchmarkServerAsync(SessionMediumQuery);

        public string SessionMediumQuery { get; } = Resources.SessionMediumQuery;

        [Benchmark]
        public async Task Sessions_Large() =>
            await BenchmarkServerAsync(SessionLargeQuery);

        public string SessionLargeQuery { get; } = Resources.SessionLargeQuery;
    }
}
